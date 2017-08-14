using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimecardLogic.DataModels
{
    public struct Yyyymm
    {
        public Yyyymm(int year, int month) : this()
        {
            Year = year;
            Month = month;
        }

        public static Yyyymm Empty { get { return new Yyyymm(0, 0); } }
        public int Year { get; }
        public int Month { get; }
        public bool IsEmpty { get { return Year == 0 && Month == 0; } }

        public static Yyyymm Parse(string yyyymm, string timeZoneId)
        {
            if ("今月".Equals(yyyymm))
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var nowTz = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                return Yyyymm.FromDate(nowTz);
            }
            else if ("先月".Equals(yyyymm))
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var nowTz = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                return Yyyymm.FromDate(nowTz.AddMonths(-1));
            }

            int year = 0;
            int month = 0;

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
                return Yyyymm.Empty;
            }
            return new Yyyymm(year, month);
        }

        public static Yyyymm FromDate(DateTime dateTime)
        {
            return new Yyyymm(dateTime.Year, dateTime.Month);
        }

        public string Format()
        {
            return $"{Year}年{Month}月";
        }
    }
}
