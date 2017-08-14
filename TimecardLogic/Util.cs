using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimecardLogic.DataModels;

namespace TimecardLogic
{
    public static class Util
    {
        public static Hhmm ParseHHMM(string hhmm)
        {
            int hour = 0;
            int minute = 0;

            if (hhmm.Length != 4)
            {
                return Hhmm.Empty;
            }

            hour = int.Parse(hhmm.Substring(0, 2));
            minute = int.Parse(hhmm.Substring(2));
            return new Hhmm(hour, minute);
        }

        public static bool ParseYYYYMM(string yyyymm, out int year, out int month)
        {
            year = 0;
            month = 0;

            var buf = yyyymm.Split('/');

            if (buf?.Length == 2)
            {
                year = int.Parse(buf[0]);
                month = int.Parse(buf[1]);
            }
            else if (yyyymm.Length >= 4)
            {
                year = int.Parse(yyyymm.Substring(0, 4));
                month = int.Parse(yyyymm.Substring(4, 2));
            }
            else
            {
                return false;
            }
            return true;
        }

        public static Yyyymmdd ParseYYYYMMDD(string yyyymmdd)
        {
            int year = 0;
            int month = 0;
            int day = 0;

            var buf = yyyymmdd.Split('/');

            if (buf?.Length == 3)
            {
                year = int.Parse(buf[0]);
                month = int.Parse(buf[1]);
                day = int.Parse(buf[2]);
            }
            else if (yyyymmdd.Length >= 8)
            {
                year = int.Parse(yyyymmdd.Substring(0, 4));
                month = int.Parse(yyyymmdd.Substring(4, 2));
                day = int.Parse(yyyymmdd.Substring(6, 2));
            }
            else
            {
                return Yyyymmdd.Empty;
            }
            return new Yyyymmdd(year, month, day);
        }

    }
}
