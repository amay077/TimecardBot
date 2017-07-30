using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimecardLogic.Entities;

namespace TimecardLogic.Repositories
{
    public sealed class ConversationStateRepository
    {
        private static string _paritionKey;
        private readonly CloudTable _conversationStateTable;

        public ConversationStateRepository()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            _paritionKey = CloudConfigurationManager.GetSetting("PartitionKey");

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            _conversationStateTable = tableClient.GetTableReference("ConversationState");
            _conversationStateTable.CreateIfNotExists();
        }

        public Task<bool> ExistsStatusByUserId(string userId)
        {
            // ユーザーIDが存在するかのクエリ
            var retrieveOperation = TableOperation.Retrieve<ConversationStateEntity>(
                _paritionKey, userId);

            // 検索実行
            return _conversationStateTable.ExecuteAsync(retrieveOperation)
                .ContinueWith(x => x.Result != null);
        }


        public Task<ConversationStateEntity> GetStatusByUserId(string userId)
        {
            // ユーザーIDが存在するかのクエリ
            var retrieveOperation = TableOperation.Retrieve<ConversationStateEntity>(
                _paritionKey, userId);

            // 検索実行
            return _conversationStateTable.ExecuteAsync(retrieveOperation)
                .ContinueWith(x => ((ConversationStateEntity)x?.Result?.Result));
        }

        public Task UpsertState(string partitionKey, string userId, AskingState state, string askingEoWTime, string targetDate)
        {
            // エンティティ作成
            var stateEntity = new ConversationStateEntity(partitionKey, userId)
            {
                State = state,
                TargetTime = askingEoWTime,
                TargetDate = targetDate
            };

            var upsertOp = TableOperation.InsertOrReplace(stateEntity);
            return _conversationStateTable.ExecuteAsync(upsertOp);
        }
        public Task UpsertState(ConversationStateEntity stateEntity)
        {
            // エンティティ作成
            var upsertOp = TableOperation.InsertOrReplace(stateEntity);
            return _conversationStateTable.ExecuteAsync(upsertOp);
        }
    }
}
