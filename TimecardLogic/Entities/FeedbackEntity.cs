using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimecardLogic.Entities
{
    public sealed class FeedbackEntity : TableEntity
    {
        public FeedbackEntity(string partitionKey, string userID)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = MakeRowkey(userID, DateTime.UtcNow);
            this.UserId = userID;
        }

        public static string MakeRowkey(string userID, DateTime utcDate)
        {
            return $"{userID}_{utcDate:yyyyMMddHHmmss}";
        }


        public string UserId { get; set; }
        public string Body { get; set; }
    }
}
