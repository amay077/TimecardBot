using CSharp.Japanese.Kanaxs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimecardLogic.DataModels
{
    [Serializable]

    public struct Yyyymmdd
    {
        public int Year { get; }
        public int Month { get; }
        public int Day { get; }
        public static Yyyymmdd Empty { get { return new Yyyymmdd(0, 0, 0); } }

        public Yyyymmdd(int year, int month, int day) : this()
        {
            Year = year;
            Month = month;
            Day = day;
        }

        public static Yyyymmdd FromDate(DateTime dateTime)
        {
            return new Yyyymmdd(dateTime.Year, dateTime.Month, dateTime.Day);
        }

        public static Yyyymmdd Parse(string yyyymmdd, string timeZoneId)
        {
            try
            {
                int year = 0;
                int month = 0;
                int day = 0;

                if ("今日".Equals(yyyymmdd))
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                    var nowTz = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                    return Yyyymmdd.FromDate(nowTz);
                }
                else if ("昨日".Equals(yyyymmdd))
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                    var nowTz = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                    return Yyyymmdd.FromDate(nowTz.AddDays(-1));
                }

                var hankaku = Kana.ToHankaku(yyyymmdd);

                var buf = hankaku.Split('/', '-', '年', '月', '日');

                if (buf?.Length == 3 || buf?.Length == 4)
                {
                    year = int.Parse(buf[0]);
                    month = int.Parse(buf[1]);
                    day = int.Parse(buf[2]);
                }
                else if (hankaku.Length >= 8)
                {
                    year = int.Parse(hankaku.Substring(0, 4));
                    month = int.Parse(hankaku.Substring(4, 2));
                    day = int.Parse(hankaku.Substring(6, 2));
                }
                else
                {
                    return Yyyymmdd.Empty;
                }

                if (!(2000 <= year && year <= 3999))
                {
                    Trace.WriteLine($"Yyyymm parse failed - year is out of range {yyyymmdd}");
                    return Yyyymmdd.Empty;
                }

                if (!(1 <= month && month <= 12))
                {
                    Trace.WriteLine($"Yyyymm parse failed - month is out of range {yyyymmdd}");
                    return Yyyymmdd.Empty;
                }

                if (!(1 <= day && day <= 31))
                {
                    Trace.WriteLine($"Yyyymm parse failed - day is out of range {yyyymmdd}");
                    return Yyyymmdd.Empty;
                }

                return new Yyyymmdd(year, month, day);
            }
            catch (Exception)
            {
                Trace.WriteLine($"Hhmm parse failed - {yyyymmdd}, timeZone - {timeZoneId}");
                return Yyyymmdd.Empty;
            }
        }


        public bool isEmpty
        {
            get
            {
                return Year == 0 && Month == 0 && Day == 0;
            }
        }

        public override bool Equals(object obj)
        {
            var ymd = obj as Yyyymmdd?;
            if (!ymd.HasValue)
            {
                return false;
            }

            return Year == ymd.Value.Year && Month == ymd.Value.Month && Day == ymd.Value.Day;
        }

        public string FormatMd()
        {
            return $"{Month}月{Day}日";
        }
    }
}
