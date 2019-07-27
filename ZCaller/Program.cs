using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZCaller
{
    class Program
    {
        static void Main(string[] args)
        {
            var v = PomodoroDatabase.DBSingleton.getInstance().GetPmodoroCount(DateTime.Parse("2019-07-24"));

        }
    }
}
