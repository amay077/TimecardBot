using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimecardLogic.DataModels;
using TimecardLogic.Entities;

namespace TimecardLogic.Repositories
{
    public sealed class MonthlyTimecardRepository
    {
        private static string _paritionKey;
        private readonly CloudTable _monthlyTimecardTable;

        public MonthlyTimecardRepository()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            _paritionKey = CloudConfigurationManager.GetSetting("PartitionKey");

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            _monthlyTimecardTable = tableClient.GetTableReference("MonthlyTimecard");
            _monthlyTimecardTable.CreateIfNotExists();
        }

        public async Task<MonthlyTimecardEntity> GetMonthlyTimecardsByYearMonth(string userId, int year, int month)
        {
            // ユーザーIDが存在するかのクエリ
            var retrieveOp = TableOperation.Retrieve<MonthlyTimecardEntity>(
                _paritionKey, MonthlyTimecardEntity.MakeRowkey(userId, $"{year:0000}{month:00}"));

            // 検索実行
            var taskResult = await _monthlyTimecardTable.ExecuteAsync(retrieveOp);

            if (taskResult.Result != null)
            {
                return (MonthlyTimecardEntity)taskResult.Result;
            }
            else
            {
                return null;
            }
        }

        public async Task<IList<TimecardRecord>> GetTimecardRecordByYearMonth(string userId, Yyyymm ym)
        {
            IList<TimecardRecord> timecardRecords = new List<TimecardRecord>();
            var monthlyTimecardEntity = await GetMonthlyTimecardsByYearMonth(userId, ym.Year, ym.Month);

            if (monthlyTimecardEntity != null)
            {
                timecardRecords = monthlyTimecardEntity.GetTimecardsAsList();
            }
            return timecardRecords;
        }


        public async Task UpsertTimecardRecord(string userId, Yyyymmdd ymd, Hhmm hm)
        {
            // 既存の月次タイムカードを得る
            var monthlyTimecardEntity = await GetMonthlyTimecardsByYearMonth(userId, ymd.Year, ymd.Month);
            IList<TimecardRecord> timecardRecords = new List<TimecardRecord>();
            if (monthlyTimecardEntity == null)
            {
                // 得られなかったら新たに月次タイムカードを作る
                monthlyTimecardEntity = new MonthlyTimecardEntity(_paritionKey, userId, $"{ymd.Year:0000}{ymd.Month:00}");
            }
            else
            {
                // 得られたら、月次タイムカード内にJsonで入っている各日のタイムカード情報をデシリアライズして得る
                timecardRecords = monthlyTimecardEntity.GetTimecardsAsList();
            }

            // 月次タイムカード内の各日のタイムカード群から該当日を検索する
            var hit = timecardRecords.FirstOrDefault(x => x.Day == ymd.Day);
            if (hit != null)
            {
                // 見つかればその終業時刻を書き換え
                hit.EoWTime = $"{hm.Hour:00}{hm.Minute:00}";
            }
            else
            {
                // 見つからなければ新たにタイムカードレコードを作って追加
                var newRecord = new TimecardRecord()
                {
                    Day = ymd.Day,
                    EoWTime = $"{hm.Hour:00}{hm.Minute:00}"
                };
                timecardRecords.Add(newRecord);
            }

            // Jsonにシリアライズして戻す
            monthlyTimecardEntity.SetTimecardDataJsonFromList(timecardRecords);


            // 月次タイムカードをUpsertする
            var upsertOp = TableOperation.InsertOrReplace(monthlyTimecardEntity);
            await _monthlyTimecardTable.ExecuteAsync(upsertOp);
        }

        public async Task DeleteTimecardRecord(string userId, Yyyymmdd ymd)
        {
            // 既存の月次タイムカードを得る
            var monthlyTimecardEntity = await GetMonthlyTimecardsByYearMonth(userId, ymd.Year, ymd.Month);
            IList<TimecardRecord> timecardRecords = new List<TimecardRecord>();
            if (monthlyTimecardEntity == null)
            {
                // 見つからなければ終わり
                return;
            }
            else
            {
                // 得られたら、月次タイムカード内にJsonで入っている各日のタイムカード情報をデシリアライズして得る
                timecardRecords = monthlyTimecardEntity.GetTimecardsAsList();
            }

            // 月次タイムカード内の各日のタイムカード群から該当日を検索する
            var hit = timecardRecords.FirstOrDefault(x => x.Day == ymd.Day);
            if (hit != null)
            {
                // 見つかればそれをリストから削除
                timecardRecords.Remove(hit);
            }
            else
            {
                // 見つからなければ終わり
                return;
            }

            // Jsonにシリアライズして戻す
            monthlyTimecardEntity.SetTimecardDataJsonFromList(timecardRecords);


            // 月次タイムカードをUpsertする
            var upsertOp = TableOperation.InsertOrReplace(monthlyTimecardEntity);
            await _monthlyTimecardTable.ExecuteAsync(upsertOp);
        }
    }
}
