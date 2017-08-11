using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TimecardLogic.DataModels;
using TimecardLogic.Entities;

namespace TimecardLogic.Repositories
{
    public sealed class UsersRepository
    {
        private static string _paritionKey;

        public static string PartitionKey { get { return _paritionKey; } }

        private readonly CloudTable _usersTable;

        public UsersRepository()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            _paritionKey = CloudConfigurationManager.GetSetting("PartitionKey");

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Retrieve a reference to the table.
            _usersTable = tableClient.GetTableReference("Users");

            // Create the table if it doesn't exist.
            _usersTable.CreateIfNotExists();
        }

        public async Task<IEnumerable<UserEntity>> GetAllUsers()
        {
            var result = new List<UserEntity>();

            // Initialize a default TableQuery to retrieve all the entities in the table.
            var tableQuery = new TableQuery<UserEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, _paritionKey));

            // Initialize the continuation token to null to start from the beginning of the table.
            TableContinuationToken continuationToken = null;

            do
            {
                // Retrieve a segment (up to 1,000 entities).
                var tableQueryResult =
                    await _usersTable.ExecuteQuerySegmentedAsync(tableQuery, continuationToken);

                // Assign the new continuation token to tell the service where to
                // continue on the next iteration (or null if it has reached the end).
                continuationToken = tableQueryResult.ContinuationToken;

                // Print the number of rows retrieved.
                Trace.WriteLine("Rows retrieved {0}", tableQueryResult.Results.Count.ToString());

                foreach (var item in tableQueryResult.Results)
                {
                    result.Add(item);
                }

                // Loop until a null continuation token is received, indicating the end of the table.
            } while (continuationToken != null);

            return result;
        }

        public Task<UserEntity> GetUserEntityById(string userId)
        {
            // ユーザーIDが存在するかのクエリ
            var retrieveOperation = TableOperation.Retrieve<UserEntity>(
                _paritionKey, userId);

            // 検索実行
            return _usersTable.ExecuteAsync(retrieveOperation)
                .ContinueWith(x =>
                {
                    return ((UserEntity)x?.Result?.Result);
                });
        }


        /// <summary>
        /// ユーザーIDが既に存在するなら true
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<User> GetUserById(string userId)
        {
            var userEntity = await GetUserEntityById(userId);
            return userEntity.ToModel();
        }

        public Task AddUser(string userId, string nickName, string askEndOfWorkStartTime, string askEndOfWorkEndTime, string timeZoneId, string conversationRef,
            string dayOfWeekEnables, IList<string> holidays)
        {
            // エンティティ作成
            var user = new UserEntity(_paritionKey, userId)
            {
                NickName = nickName,
                AskEndOfWorkStartTime = askEndOfWorkStartTime,
                AskEndOfWorkEndTime = askEndOfWorkEndTime,
                TimeZoneId = timeZoneId,
                ConversationRef = conversationRef,
                DayOfWeekEnables = dayOfWeekEnables,
                HolidaysJson = User.GetHolidaysJsonFromList(holidays)
            };

            // Create the TableOperation object that inserts the customer entity.
            var insertOperation = TableOperation.Insert(user);

            // Execute the insert operation.
            return _usersTable.ExecuteAsync(insertOperation);
        }

        public async Task DeleteUser(string userId)
        {
            // 削除対象User取得
            var retrieveOp = TableOperation.Retrieve<UserEntity>(
                _paritionKey, userId);

            // 検索実行
            var result = await _usersTable.ExecuteAsync(retrieveOp);
            var user = result.Result as UserEntity;

            // Execute the insert operation.
            await _usersTable.ExecuteAsync(TableOperation.Delete(user));
        }
    }
}