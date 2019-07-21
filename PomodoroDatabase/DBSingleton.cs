using SQLite;
using System;
using System.Collections.Generic;
using System.IO;

namespace PomodoroDatabase
{
    public class DBSingleton
    {
        const string CANCELED = "CANCELED";
        const string COMPLETE = "COMPLETE";

        string dbFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PomodoroDB.sqlite");

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

        public void CompletePomodoro()
        {
            DatabaseLink.Execute("update Pomodoro set status='" + COMPLETE + "', enddate = DATETIME('now') where enddate is null;");
        }

        public List<Pomodoro> GetPomodoros(string status)
        {
            throw new NotImplementedException("make GetPomodoros...");
        }

        public void CreateDB()
        {
            var db = new SQLiteConnection(dbFileName);
            db.CreateTable<Pomodoro>();

            db.Close();
        }

    }

}
