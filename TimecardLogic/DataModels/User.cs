using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimecardLogic.DataModels
{
    /// <summary>
    /// ユーザーのシリアル可能なデータクラス
    /// </summary>
    /// <remarks>
    /// UserEntity は TableEntity がシリアル化できないため、これを用意した。
    /// </remarks>
    [Serializable]
    public class User
    {
        public static string[] WEEKDAYS = new[] { "日", "月", "火", "水", "木", "金", "土" };

        public string UserId { get; } // getter がないと Table Storage に列が追加されなかった

        public string NickName { get; }

        // 終業したか聞き始める時刻(HHMM)
        public string AskEndOfWorkStartTime { get;  }

        // 終業したか聞き終わる時刻(HHMM)
        public string AskEndOfWorkEndTime { get;  }

        // タイムゾーン
        public string TimeZoneId { get;  }

        // Json化された Conversation
        public string ConversationRef { get; }
        public string DayOfWeekEnables { get; }
        public string HolidaysJson { get; }

        public User(string userId, string nickName,
            string askEndOfWorkStartTime, string askEndOfWorkEndTime, 
            string timeZoneId, string conversationRef,
            string dayOfWeekEnables, string holidaysJson)
        {
            UserId = userId;
            NickName = nickName;
            AskEndOfWorkStartTime = askEndOfWorkStartTime;
            AskEndOfWorkEndTime = askEndOfWorkEndTime;
            TimeZoneId = timeZoneId;
            ConversationRef = conversationRef;
            DayOfWeekEnables = dayOfWeekEnables;
            HolidaysJson = holidaysJson;
        }

        public IList<string> GetHolidaysAsList()
        {
            return GetHolidaysListFromJson(HolidaysJson);
        }

        public static IList<string> GetHolidaysListFromJson(string holidaysJson)
        {
            if (holidaysJson == null)
            {
                return default(IList<string>);
            }

            var holydays = new List<string>();
            var holidayArray = (JArray)JsonConvert.DeserializeObject(holidaysJson);
            if (holidayArray != null)
            {
                foreach (var item in holidayArray.ToObject<List<string>>())
                {
                    holydays.Add(item);
                }
            }
            return holydays;
        }

        public static string GetHolidaysJsonFromList(IList<string> holidays)
        {
            if (holidays == null)
            {
                return string.Empty;
            }

            return JsonConvert.SerializeObject(holidays.ToArray<string>());
        }

        public string ToDescribeString()
        {
            var builder = new StringBuilder();

            var offWeekDays = string.Join(string.Empty, WEEKDAYS.Zip(DayOfWeekEnables.ToCharArray(), (label, flag) => 
            {
                return flag.Equals('0') ? label : string.Empty;
            }));

            offWeekDays = string.IsNullOrEmpty(offWeekDays) ? "なし" : offWeekDays;

            builder.Append($"ニックネーム: {NickName}\n\n");
            builder.Append($"終業時刻（確認開始時刻）: {AskEndOfWorkStartTime}\n\n");
            builder.Append($"確認終了時刻: {AskEndOfWorkEndTime}\n\n");
            builder.Append($"休みの曜日: {offWeekDays}\n\n");
            builder.Append($"タイムゾーン: {TimeZoneId}\n\n");

            return builder.ToString();
        }

    }
}
