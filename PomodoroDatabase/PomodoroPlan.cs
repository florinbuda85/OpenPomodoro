using SQLite;
using System;

namespace PomodoroDatabase
{
    public class PomodoroPlan
    {
        [PrimaryKey, AutoIncrement]
        public int id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedDate { get; set; }
        public int PomodoroCount { get; set; }
    }
}
