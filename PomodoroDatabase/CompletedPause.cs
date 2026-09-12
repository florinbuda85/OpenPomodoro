using SQLite;
using System;

namespace PomodoroDatabase
{
    public class CompletedPause
    {
        [PrimaryKey, AutoIncrement]
        public int id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsLong { get; set; }
        public int SecondsUntilWork { get; set; }
    }
}
