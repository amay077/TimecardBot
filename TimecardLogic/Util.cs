using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimecardLogic
{
    public static class Util
    {
        public static bool ParseHHMM(string hhmm, out int hour, out int minute)
        {
            hour = 0;
            minute = 0;

            if (hhmm.Length != 4)
            {
                return false;
            }

            hour = int.Parse(hhmm.Substring(0, 2));
            minute = int.Parse(hhmm.Substring(2));
            return true;
        }

    }
}
