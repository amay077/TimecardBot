using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimecardLogic.Entities
{
    public sealed class ConversationStateEntity : TableEntity
    {
        public string UserId { get; set; }

        public AskingState State { get; set; }

        public string StateText {
            get { return (string)Enum.GetName(typeof(AskingState), State); }
            set { State = (AskingState)Enum.Parse(typeof(AskingState), value, true); } }

        public string TargetTime { get; set; }

        public string TargetDate { get; set; } // 最終打刻日

        public ConversationStateEntity(string partitionKey, string userID)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = userID;
            this.UserId = userID;
        }

        public ConversationStateEntity() { }

    }
}
