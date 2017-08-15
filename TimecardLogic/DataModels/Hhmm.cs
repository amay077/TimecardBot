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
    public struct Hhmm
    {
        internal static readonly Hhmm Empty = new Hhmm(0, 0);

        public int Hour { get; }
        public int Minute { get; }
        public bool IsEmpty
        {
            get
            {
                return Hour == 0 && Minute == 0;
            }
        }

        public Hhmm(int hour, int minute) : this()
        {
            this.Hour = hour;
            this.Minute = minute;
        }

        public static Hhmm Parse(string hhmm)
        {
            try
            {
                int hour = 0;
                int minute = 0;

                var hankaku = Kana.ToHankaku(hhmm);
                var buf = hankaku.Split(':', '-', '時', '分', '秒');

                if (buf?.Length == 2 || buf?.Length == 3 || buf?.Length == 4)
                {
                    hour = int.Parse(buf[0]);
                    minute = int.Parse(buf[1]);
                }
                if (hhmm.Length >= 4)
                {
                    hour = int.Parse(hhmm.Substring(0, 2));
                    minute = int.Parse(hhmm.Substring(2));
                }
                else
                {
                    return Hhmm.Empty;
                }

                if (!(0 <= hour && hour <= 36))
                {
                    Trace.WriteLine($"Hhmm parse failed - hour is out of range {hhmm}");
                    return Hhmm.Empty;
                }

                if (!(0 <= minute && minute <= 59))
                {
                    Trace.WriteLine($"Hhmm parse failed - minute is out of range {hhmm}");
                    return Hhmm.Empty;
                }

                return new Hhmm(hour, minute);
            }
            catch (Exception)
            {
                Trace.WriteLine($"Hhmm parse failed - {hhmm}");
                return Hhmm.Empty;
            }
        }

        public string Format()
        {
            return $"{Hour}時{Minute:00}分";
        }
    }
}
