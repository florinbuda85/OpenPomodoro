using System;

namespace PomodoroDatabase
{
    public class DayActivity
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsPause { get; set; }
        public string PlanText { get; set; }
    }
}
