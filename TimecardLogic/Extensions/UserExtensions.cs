using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimecardLogic.DataModels;

namespace TimecardLogic.Extensions
{
    public static class UserExtensions
    {
        public static DateTime NowAsUserTz(this User self)
        {
            var nowUtc = DateTime.UtcNow;
            var tzUser = TimeZoneInfo.FindSystemTimeZoneById(self.TimeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tzUser); // ユーザーのタイムゾーンでの現在時刻
        }
    }
}
