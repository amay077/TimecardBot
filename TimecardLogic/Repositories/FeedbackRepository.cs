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
    public sealed class FeedbackRepository
    {
        private static string _paritionKey;
        private readonly CloudTable _feedbackTable;

        public FeedbackRepository()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            _paritionKey = CloudConfigurationManager.GetSetting("PartitionKey");

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            _feedbackTable = tableClient.GetTableReference("Feedback");
            _feedbackTable.CreateIfNotExists();
        }

        public Task AddFeedback(string userId, string body)
        {
            // エンティティ作成
            var user = new FeedbackEntity(_paritionKey, userId)
            {
                Body = body
            };

            // Create the TableOperation object that inserts the customer entity.
            var insertOperation = TableOperation.Insert(user);

            // Execute the insert operation.
            return _feedbackTable.ExecuteAsync(insertOperation);
        }

    }
}
