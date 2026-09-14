using SQLite;
using System;

namespace PomodoroDatabase
{
    public class CanceledPause
    {
        [PrimaryKey, AutoIncrement]
        public int id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
