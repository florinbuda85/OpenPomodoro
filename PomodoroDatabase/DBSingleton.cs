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

        public int? StartPomodoro()
        {
            DateTime now = DateTime.Now;
            int? secondsBetweenPomodoros = GetSecondsSinceLastCompletedPomodoro(now);
            CancelUnfinishedPomodoros(now);

            // Insert only StartDate so EndDate remains NULL until the Pomodoro finishes.
            // SQLite-net would otherwise persist DateTime.MinValue for the non-nullable
            // EndDate property, making the completion update miss this row.
            DatabaseLink.Execute("insert into Pomodoro (startdate) values (?);", now);
            return secondsBetweenPomodoros;
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
            DatabaseLink.Execute("update Pomodoro set status = ?, enddate = ? where status is null;", COMPLETE, DateTime.Now);
        }

        public void StartPause()
        {
            currentPauseStartDate = DateTime.Now;
        }

        public void CancelPause()
        {
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
