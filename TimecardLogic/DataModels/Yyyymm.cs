using CSharp.Japanese.Kanaxs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            var hankaku = Kana.ToHankaku(yyyymm);
            var buf = hankaku.Split('/', '-', '年', '月', '日');

            try
            {
                if (buf?.Length == 2 || buf?.Length == 3)
                {
                    year = int.Parse(buf[0]);
                    month = int.Parse(buf[1]);
                }
                else if (hankaku.Length >= 4)
                {
                    year = int.Parse(hankaku.Substring(0, 4));
                    month = int.Parse(hankaku.Substring(4, 2));
                }
                else
                {
                    Trace.WriteLine($"Yyyymm parse invalid length - {yyyymm}");
                    return Yyyymm.Empty;
                }
                return new Yyyymm(year, month);
            }
            catch (Exception)
            {
                Trace.WriteLine($"Yyyymm parse failed - {yyyymm}");
                return Yyyymm.Empty;
            }
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
