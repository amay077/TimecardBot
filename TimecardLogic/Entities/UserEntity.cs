using Microsoft.WindowsAzure.Storage.Table;
using TimecardLogic.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using Microsoft.WindowsAzure.Storage;
using TimecardLogic.DataModels;

namespace TimecardLogic.Entities
{
    public sealed class UserEntity : TableEntity
    {
        public UserEntity(string partitionKey, string userID)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = userID;
            this.UserId = userID;
        }

        public UserEntity() { }

        public string UserId { get; set; } // getter がないと Table Storage に列が追加されなかった

        public string NickName { get; set; }

        // 終業したか聞き始める時刻(HHMM)
        public string AskEndOfWorkStartTime { get; set; }

        // 終業したか聞き終わる時刻(HHMM)
        public string AskEndOfWorkEndTime { get; set; }

        // タイムゾーン
        public string TimeZoneId { get; set; }

        // Json化された Conversation
        public string ConversationRef { get; set; }

        /// <summary>
        /// 曜日毎の有効無効(0 or 1:有効、日月火水木金土)
        /// </summary>
        public string DayOfWeekEnables { get; set; }

        public string HolidaysJson { get; set; }

        public User ToModel()
        {
            return new User(UserId, NickName, AskEndOfWorkStartTime, AskEndOfWorkEndTime, TimeZoneId, ConversationRef, DayOfWeekEnables, HolidaysJson);
        }
    }
}