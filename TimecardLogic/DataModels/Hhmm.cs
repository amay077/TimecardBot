using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimecardLogic.DataModels
{
    public struct Hhmm
    {
        internal static readonly Hhmm Empty = new Hhmm(0, 0);

        public int Hour { get; }
        public int Minute { get; }
        public bool IsEmpty {
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

        public string Format()
        {
            return $"{Hour}時{Minute:00}分";
        }
    }
}
