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

        public User(string userId, string nickName,
            string askEndOfWorkStartTime, string askEndOfWorkEndTime, 
            string timeZoneId, string conversationRef)
        {
            UserId = userId;
            NickName = nickName;
            AskEndOfWorkStartTime = askEndOfWorkStartTime;
            AskEndOfWorkEndTime = askEndOfWorkEndTime;
            TimeZoneId = timeZoneId;
            ConversationRef = conversationRef;
        }

    }
}
