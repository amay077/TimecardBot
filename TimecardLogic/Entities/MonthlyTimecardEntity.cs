using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimecardLogic.DataModels;

namespace TimecardLogic.Entities
{
    public sealed class MonthlyTimecardEntity : TableEntity
    {
        public string UserId { get; set; }

        public string YearMonth { get; set; }

        public string TimecardDataJson { get; set; }

        public static string MakeRowkey(string userID, string yyyymm)
        {
            return $"{userID}_{yyyymm}";
        }

        public MonthlyTimecardEntity(string partitionKey, string userID, string yyyymm)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = MakeRowkey(userID, yyyymm);
            this.UserId = userID;
            this.YearMonth = yyyymm;
        }

        public MonthlyTimecardEntity()
        {
        }

        public IList<TimecardRecord> GetTimecardsAsList()
        {
            var timecardRecords = new List<TimecardRecord>();
            var timecardRecordArray = (JArray)JsonConvert.DeserializeObject(TimecardDataJson);
            if (timecardRecordArray != null)
            {
                foreach (var item in timecardRecordArray.ToObject<List<TimecardRecord>>())
                {
                    timecardRecords.Add(item);
                }
            }
            return timecardRecords;
        }

        public void SetTimecardDataJsonFromList(IList<TimecardRecord> timecardRecords)
        {
            TimecardDataJson = JsonConvert.SerializeObject(timecardRecords.ToArray<TimecardRecord>());
        }
    }
}
