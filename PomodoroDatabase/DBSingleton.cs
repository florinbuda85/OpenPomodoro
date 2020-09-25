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

        private DBSingleton()
        {
            if (!File.Exists(dbFileName))
            {
                CreateDB();
            }

            DatabaseLink = new SQLiteConnection(dbFileName);
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

        public void StartPomodoro()
        {
            DatabaseLink.Execute("update Pomodoro set status='" + CANCELED + "', enddate = DATETIME('now') where enddate is null;");
            DatabaseLink.Execute("insert into Pomodoro (startdate) values ( DATETIME('now'));");
        }

        public Tuple<string, string> GetChartData(DateTime date)
        {
            DateTime myDate = new DateTime(date.Year, date.Month, 1);
            StringBuilder dates = new StringBuilder();
            StringBuilder counts = new StringBuilder();

            while (myDate.Month == date.Month)
            {
                dates.Append(dates.Length == 0 ? ("'" + myDate.ToString("MMM dd") + "'") : (",'" + myDate.ToString("MMM dd") + "'"));
                counts.Append(counts.Length == 0 ? ("'" + GetPmodoroCount(myDate) + "'") : (",'" + GetPmodoroCount(myDate) + "'"));

                myDate = myDate.AddDays(1);
            }

            return new Tuple<string, string>(dates.ToString(), counts.ToString());
        }

        public int GetPmodoroCount(DateTime d)
        {
            string s = "select * from pomodoro where status = 'COMPLETE' and strftime('%Y-%m-%d', startdate) = '" + d.ToString("yyyy-MM-dd") + "';";

            return DatabaseLink.Query<Pomodoro>(s)
                .Count();
        }

        public void CompletePomodoro()
        {
            DatabaseLink.Execute("update Pomodoro set status='" + COMPLETE + "', enddate = DATETIME('now') where enddate is null;");
        }

        public void CreateDB()
        {
            var db = new SQLiteConnection(dbFileName);
            db.CreateTable<Pomodoro>();
            db.CreateTable<PauseAdvice>();
            db.Close();
        }

        public void InsertAdvice(string advice)
        {
            var safeAdvice = advice.Replace("'", "*");

            try
            {
                if ( DatabaseLink.Query<Pomodoro>($"select id from pauseadvice where content = '{safeAdvice}';").Count() == 0)
                {
                    DatabaseLink.Execute($"insert into PauseAdvice (content, probability) values ('{safeAdvice}','10');");
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public string GetAdvice()
        {
            try
            {
                string s = "SELECT * FROM PauseAdvice ORDER BY lastseen LIMIT 5";
                List<PauseAdvice> bList = new List<PauseAdvice>();
                var top5 = DatabaseLink.Query<PauseAdvice>(s);

                top5.ForEach(x =>
                {
                    int i = 0;
                    while (++i < x.Probability)
                    {
                        bList.Add(x);
                    }
                });

                var randomAdvice = bList.ElementAt((new Random()).Next(0, bList.Count - 1));

                var u = "update pauseadvice set lastseen = DATETIME('now') where id = " + randomAdvice.id;
                DatabaseLink.Execute(u);

                return randomAdvice.Content;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }

}
