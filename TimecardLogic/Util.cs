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

        public static bool ParseYYYYMMDD(string yyyymmdd, out int year, out int month, out int day)
        {
            year = 0;
            month = 0;
            day = 0;

            var buf = yyyymmdd.Split('/');

            if (buf?.Length == 3)
            {
                year = int.Parse(buf[0]);
                month = int.Parse(buf[1]);
                day = int.Parse(buf[2]);
            }
            else if (yyyymmdd.Length != 6)
            {
                year = int.Parse(yyyymmdd.Substring(0, 4));
                month = int.Parse(yyyymmdd.Substring(4, 2));
                day = int.Parse(yyyymmdd.Substring(6, 2));
            }
            return true;
        }

    }
}
