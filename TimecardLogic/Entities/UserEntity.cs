using Microsoft.WindowsAzure.Storage.Table;
using TimecardLogic.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimecardLogic.Entities
{
    public class UserEntity : TableEntity
    {
        public UserEntity(string partitionKey, string userID)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = userID;
            this.UserId = userID;
        }

        public UserEntity() { }

        public string UserId { get; set;  } // getter がないと Table Storage に列が追加されなかった

        public string NickName { get; set; }

        // 終業したか聞き始める時刻(HHMM)
        public string AskEndOfWorkStartTime { get; set; }

        // 終業したか聞き終わる時刻(HHMM)
        public string AskEndOfWorkEndTime { get; set; }

        // タイムゾーン
        public string TimeZoneId { get; set; }
    }
}