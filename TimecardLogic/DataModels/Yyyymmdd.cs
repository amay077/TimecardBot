using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimecardLogic.DataModels
{
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

        public bool isEmpty
        {
            get
            {
                return Year == 0 && Month == 0 && Day == 0;
            }
        }
    }
}
