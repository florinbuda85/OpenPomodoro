using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenPomodoro
{
    public class WStates
    {
        public const int DEFAULT = 0;
        public const int WORKING = 10;
        public const int FINISHED_WORK = 20;
        public const int PAUSING = 30;
        public const int PAUSING_LONG = 31;

        public const int FINISHED_PAUSE = 40;
        public const int STOP = 60;
        public const int CANCEL = 70;

        public const int ALERTING = 50;


    }
}
