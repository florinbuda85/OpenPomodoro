using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PomodoroDatabase
{
    public class PauseAdvice
    {
        [PrimaryKey, AutoIncrement]
        public int id { get; set; }

        public string Content { get; set; }

        public int Probability { get; set; }

        public DateTime LastSeen { get; set; }

        public string Type { get; set; }

        public int SortOrder { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime CompletedDate { get; set; }

        [Ignore]
        public bool IsOnce => string.Equals(Type, "Once", StringComparison.OrdinalIgnoreCase);
    }
}
