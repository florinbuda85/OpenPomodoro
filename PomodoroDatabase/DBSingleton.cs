using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PomodoroDatabase
{
    public class DBSingleton
    {
        const string CANCELED = "CANCELED";
        const string COMPLETE = "COMPLETE";
        readonly string dbFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PomodoroDB.sqlite");

        SQLiteConnection DatabaseLink;
        DateTime? currentPauseStartDate;

        private DBSingleton()
        {
            DatabaseLink = new SQLiteConnection(dbFileName);
            DatabaseLink.CreateTable<Pomodoro>();
            DatabaseLink.CreateTable<PauseAdvice>();
            DatabaseLink.CreateTable<CompletedPause>();
            DatabaseLink.CreateTable<CanceledPause>();
            DatabaseLink.CreateTable<PomodoroPlan>();
            NormalizePauseReminderOrder();
            CancelUnfinishedPomodoros(DateTime.Now);
        }

        private static DBSingleton _instance = null;

        public static DBSingleton getInstance()
        {
            if (_instance == null)
            {
                _instance = new DBSingleton();
            }
            return _instance;
        }

        /****/

        public int? StartPomodoro(int? planId = null)
        {
            if (planId.HasValue && GetPomodoroPlan(planId.Value) == null)
            {
                throw new ArgumentException("The selected plan no longer exists.", nameof(planId));
            }

            DateTime now = DateTime.Now;
            int? secondsBetweenPomodoros = GetSecondsSinceLastCompletedPomodoro(now);
            CancelUnfinishedPomodoros(now);

            // Insert only StartDate so EndDate remains NULL until the Pomodoro finishes.
            // SQLite-net would otherwise persist DateTime.MinValue for the non-nullable
            // EndDate property, making the completion update miss this row.
            DatabaseLink.Execute(
                "insert into Pomodoro (startdate, planid) values (?, ?);",
                now,
                planId);
            return secondsBetweenPomodoros;
        }

        public int CreatePomodoroPlan(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Plan content cannot be empty.", nameof(content));
            }

            PomodoroPlan plan = new PomodoroPlan
            {
                Content = content.Trim(),
                CreatedDate = DateTime.Now,
                PomodoroCount = 0
            };
            DatabaseLink.Insert(plan);
            return plan.id;
        }

        public List<PomodoroPlan> GetAllPomodoroPlans()
        {
            return DatabaseLink.Table<PomodoroPlan>()
                .OrderByDescending(plan => plan.PomodoroCount)
                .ThenByDescending(plan => plan.id)
                .ToList();
        }

        public PomodoroPlan GetPomodoroPlan(int id)
        {
            return DatabaseLink.Find<PomodoroPlan>(id);
        }

        public void UpdatePomodoroPlan(int id, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Plan content cannot be empty.", nameof(content));
            }

            // Update only the text so accumulated counts cannot be overwritten by an older view.
            DatabaseLink.Execute("update PomodoroPlan set Content = ? where id = ?;", content.Trim(), id);
        }

        public void DeletePomodoroPlan(int id)
        {
            DatabaseLink.RunInTransaction(() =>
            {
                // Keep session history and chart totals when a plan is deleted.
                DatabaseLink.Execute("update Pomodoro set PlanId = NULL where PlanId = ?;", id);
                DatabaseLink.Delete<PomodoroPlan>(id);
            });
        }

        public void CancelPomodoro()
        {
            CancelUnfinishedPomodoros(DateTime.Now);
        }

        private void CancelUnfinishedPomodoros(DateTime canceledAt)
        {
            DatabaseLink.Execute(
                "update Pomodoro set status = ?, enddate = ? where status is null;",
                CANCELED,
                canceledAt);
        }

        private int? GetSecondsSinceLastCompletedPomodoro(DateTime nextPomodoroStartDate)
        {
            Pomodoro previousPomodoro = DatabaseLink.Table<Pomodoro>()
                .Where(p => p.Status == COMPLETE && p.EndDate <= nextPomodoroStartDate)
                .OrderByDescending(p => p.EndDate)
                .FirstOrDefault();

            if (previousPomodoro == null)
            {
                return null;
            }

            return Math.Max(0, (int)(nextPomodoroStartDate - previousPomodoro.EndDate).TotalSeconds);
        }

        public Tuple<string, string> GetChartData(DateTime date)
        {
            DateTime myDate = new DateTime(date.Year, date.Month, 1);
            StringBuilder dates = new StringBuilder();
            StringBuilder counts = new StringBuilder();

            while (myDate.Month == date.Month)
            {
                dates.Append(dates.Length == 0 ? ("'" + myDate.ToString("MMM dd") + "'") : (",'" + myDate.ToString("MMM dd") + "'"));
                counts.Append(counts.Length == 0 ? ("'" + GetPomodoroCount(myDate) + "'") : (",'" + GetPomodoroCount(myDate) + "'"));

                myDate = myDate.AddDays(1);
            }

            return new Tuple<string, string>(dates.ToString(), counts.ToString());
        }

        public int GetPomodoroCount(DateTime date)
        {
            DateTime startOfDay = date.Date;
            DateTime startOfNextDay = startOfDay.AddDays(1);

            return DatabaseLink.Table<Pomodoro>()
                .Count(p => p.Status == COMPLETE && p.StartDate >= startOfDay && p.StartDate < startOfNextDay);
        }

        // Kept for compatibility with the ZCaller utility.
        public int GetPmodoroCount(DateTime date)
        {
            return GetPomodoroCount(date);
        }

        public void CompletePomodoro()
        {
            Pomodoro activePomodoro = DatabaseLink.Table<Pomodoro>()
                .Where(pomodoro => pomodoro.Status == null)
                .OrderByDescending(pomodoro => pomodoro.id)
                .FirstOrDefault();
            if (activePomodoro == null)
            {
                return;
            }

            DatabaseLink.RunInTransaction(() =>
            {
                DatabaseLink.Execute(
                    "update Pomodoro set status = ?, enddate = ? where id = ?;",
                    COMPLETE,
                    DateTime.Now,
                    activePomodoro.id);

                if (activePomodoro.PlanId.HasValue)
                {
                    DatabaseLink.Execute(
                        "update PomodoroPlan set pomodorocount = pomodorocount + 1 where id = ?;",
                        activePomodoro.PlanId.Value);
                }
            });
        }

        public void StartPause()
        {
            currentPauseStartDate = DateTime.Now;
        }

        public void CancelPause()
        {
            if (currentPauseStartDate.HasValue)
            {
                DatabaseLink.Insert(new CanceledPause
                {
                    StartDate = currentPauseStartDate.Value,
                    EndDate = DateTime.Now
                });
            }
            currentPauseStartDate = null;
        }

        public void RecordCompletedPause(bool isLongPause)
        {
            DateTime endDate = DateTime.Now;
            DatabaseLink.Insert(new CompletedPause
            {
                StartDate = currentPauseStartDate ?? endDate,
                EndDate = endDate,
                IsLong = isLongPause
            });
            currentPauseStartDate = null;
        }

        public List<DayActivity> GetDayActivity(DateTime now)
        {
            DateTime dayStart = now.Date;
            DateTime dayEnd = dayStart.AddDays(1);
            var plans = GetAllPomodoroPlans().ToDictionary(plan => plan.id);
            var intervals = DatabaseLink.Query<Pomodoro>(
                "select * from Pomodoro where StartDate < ? and (EndDate > ? or EndDate is null);",
                dayEnd, dayStart)
                .Select(work => new DayActivity
                {
                    StartDate = work.StartDate,
                    EndDate = work.Status == null ? now : work.EndDate,
                    PlanText = work.PlanId.HasValue && plans.ContainsKey(work.PlanId.Value)
                        ? plans[work.PlanId.Value].Content : null
                })
                .ToList();

            intervals.AddRange(DatabaseLink.Table<CompletedPause>()
                .Where(pause => pause.StartDate < dayEnd && pause.EndDate > dayStart)
                .ToList()
                .Select(pause => new DayActivity { StartDate = pause.StartDate, EndDate = pause.EndDate, IsPause = true }));
            intervals.AddRange(DatabaseLink.Table<CanceledPause>()
                .Where(pause => pause.StartDate < dayEnd && pause.EndDate > dayStart)
                .ToList()
                .Select(pause => new DayActivity { StartDate = pause.StartDate, EndDate = pause.EndDate, IsPause = true }));
            if (currentPauseStartDate.HasValue)
            {
                intervals.Add(new DayActivity { StartDate = currentPauseStartDate.Value, EndDate = now, IsPause = true });
            }

            return intervals.OrderBy(interval => interval.StartDate).ToList();
        }

        public List<Pomodoro> GetCompletedPomodoros(DateTime date)
        {
            DateTime startOfDay = date.Date;
            DateTime startOfNextDay = startOfDay.AddDays(1);

            return DatabaseLink.Table<Pomodoro>()
                .Where(pomodoro => pomodoro.Status == COMPLETE &&
                    pomodoro.StartDate >= startOfDay && pomodoro.StartDate < startOfNextDay)
                .OrderBy(pomodoro => pomodoro.StartDate)
                .ToList();
        }

        public List<CompletedPause> GetCompletedPauses(DateTime date)
        {
            DateTime startOfDay = date.Date;
            DateTime startOfNextDay = startOfDay.AddDays(1);

            return DatabaseLink.Table<CompletedPause>()
                .Where(pause => pause.EndDate >= startOfDay && pause.EndDate < startOfNextDay)
                .OrderBy(pause => pause.EndDate)
                .ToList();
        }

        public void CreateDB()
        {
            var db = new SQLiteConnection(dbFileName);
            db.CreateTable<Pomodoro>();
            db.CreateTable<PauseAdvice>();
            db.CreateTable<CompletedPause>();
            db.CreateTable<CanceledPause>();
            db.CreateTable<PomodoroPlan>();
            db.Close();
        }

        public void InsertAdvice(string advice, string type = "Permanent")
        {
            if (string.IsNullOrWhiteSpace(advice))
            {
                return;
            }

            DatabaseLink.Insert(new PauseAdvice
            {
                Content = advice.Trim(),
                Probability = 10,
                Type = string.Equals(type, "Once", StringComparison.OrdinalIgnoreCase) ? "Once" : "Permanent",
                SortOrder = GetNextPauseReminderOrder()
            });
        }

        public void UpdateAdvice(int id, string content, string type)
        {
            PauseAdvice reminder = DatabaseLink.Find<PauseAdvice>(id);
            if (reminder == null || string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            reminder.Content = content.Trim();
            reminder.Type = string.Equals(type, "Once", StringComparison.OrdinalIgnoreCase) ? "Once" : "Permanent";
            DatabaseLink.Update(reminder);
        }

        public List<PauseAdvice> GetAllAdvices()
        {
            try
            {
                string s = "SELECT * FROM PauseAdvice WHERE IsCompleted = 0 OR IsCompleted IS NULL ORDER BY SortOrder, id";
                return DatabaseLink.Query<PauseAdvice>(s);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void DeleteAdvice(int id)
        {
            try
            {
                var u = "delete from pauseadvice where id = " + id;
                DatabaseLink.Execute(u);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void CompleteAdvice(int id)
        {
            PauseAdvice reminder = DatabaseLink.Find<PauseAdvice>(id);
            if (reminder == null)
            {
                return;
            }

            reminder.IsCompleted = true;
            reminder.CompletedDate = DateTime.Now;
            DatabaseLink.Update(reminder);
        }

        public void MovePauseReminder(int id, int direction)
        {
            List<PauseAdvice> reminders = DatabaseLink.Table<PauseAdvice>()
                .Where(reminder => !reminder.IsCompleted)
                .ToList()
                .OrderBy(reminder => reminder.SortOrder)
                .ThenBy(reminder => reminder.id)
                .ToList();
            int currentIndex = reminders.FindIndex(reminder => reminder.id == id);
            int targetIndex = currentIndex + direction;
            if (currentIndex < 0 || targetIndex < 0 || targetIndex >= reminders.Count)
            {
                return;
            }

            int currentOrder = reminders[currentIndex].SortOrder;
            reminders[currentIndex].SortOrder = reminders[targetIndex].SortOrder;
            reminders[targetIndex].SortOrder = currentOrder;
            DatabaseLink.Update(reminders[currentIndex]);
            DatabaseLink.Update(reminders[targetIndex]);
        }

        private int GetNextPauseReminderOrder()
        {
            PauseAdvice lastReminder = DatabaseLink.Table<PauseAdvice>()
                .Where(reminder => !reminder.IsCompleted)
                .OrderByDescending(reminder => reminder.SortOrder)
                .FirstOrDefault();
            return lastReminder == null ? 1 : lastReminder.SortOrder + 1;
        }

        private void NormalizePauseReminderOrder()
        {
            List<PauseAdvice> reminders = DatabaseLink.Table<PauseAdvice>()
                .Where(reminder => !reminder.IsCompleted)
                .ToList()
                .OrderBy(reminder => reminder.SortOrder <= 0 ? int.MaxValue : reminder.SortOrder)
                .ThenBy(reminder => reminder.id)
                .ToList();

            for (int index = 0; index < reminders.Count; index++)
            {
                int expectedOrder = index + 1;
                if (reminders[index].SortOrder != expectedOrder)
                {
                    reminders[index].SortOrder = expectedOrder;
                    DatabaseLink.Update(reminders[index]);
                }
            }
        }

        public PauseAdvice GetNextPauseReminder()
        {
            PauseAdvice onceReminder = DatabaseLink.Query<PauseAdvice>(
                "SELECT * FROM PauseAdvice WHERE Type = ? AND (IsCompleted = 0 OR IsCompleted IS NULL) ORDER BY SortOrder, id LIMIT 1",
                "Once").FirstOrDefault();
            if (onceReminder != null)
            {
                return onceReminder;
            }

            return DatabaseLink.Query<PauseAdvice>(
                "SELECT * FROM PauseAdvice WHERE (Type IS NULL OR Type = '' OR Type = ?) AND (IsCompleted = 0 OR IsCompleted IS NULL) ORDER BY RANDOM() LIMIT 1",
                "Permanent").FirstOrDefault();
        }

        public void ResetAdviceViews()
        {
            var u = "update pauseadvice set probability = 10, lastseen = DATETIME('now'); ";
            DatabaseLink.Execute(u);
        }
    }

}
