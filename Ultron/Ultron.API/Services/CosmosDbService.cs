using Microsoft.Azure.Cosmos;
using System.Text.Json;

namespace Ultron.API.Services
{
    public class CosmosDbService
    {
        private readonly CosmosClient _client;
        private readonly string _databaseName;
        private readonly string _containerName;
        private Container? _container;

        public CosmosDbService(IConfiguration configuration)
        {
            var endpointUri = configuration["CosmosDb:EndpointUri"]
                ?? throw new Exception("CosmosDb EndpointUri not found.");
            var primaryKey = configuration["CosmosDb:PrimaryKey"]
                ?? throw new Exception("CosmosDb PrimaryKey not found.");

            _databaseName = configuration["CosmosDb:DatabaseName"] ?? "UltronDB";
            _containerName = configuration["CosmosDb:ContainerName"] ?? "Conversations";

            _client = new CosmosClient(endpointUri, primaryKey);
        }

        public async Task InitializeAsync()
        {
            var database = await _client.CreateDatabaseIfNotExistsAsync(_databaseName);
            var response = await database.Database.CreateContainerIfNotExistsAsync(_containerName, "/userId");
            _container = response.Container;
        }

        public async Task SaveMessageAsync(string userId, string role, string content)
        {
            if (_container == null) await InitializeAsync();

            var message = new
            {
                id = Guid.NewGuid().ToString(),
                userId,
                role,
                content,
                timestamp = DateTime.UtcNow
            };

            await _container!.CreateItemAsync(message, new PartitionKey(userId));
        }

        public async Task<List<object>> GetConversationHistoryAsync(string userId, int limit = 20)
        {
            if (_container == null) await InitializeAsync();

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.userId = @userId ORDER BY c.timestamp DESC OFFSET 0 LIMIT @limit")
                .WithParameter("@userId", userId)
                .WithParameter("@limit", limit);

            var results = new List<object>();
            var iterator = _container!.GetItemQueryIterator<dynamic>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var item in response)
                {
                    results.Add(new
                    {
                        role = (string)item.role,
                        content = (string)item.content
                    });
                }
            }

            results.Reverse();
            return results;
        }

        public async Task ClearHistoryAsync(string userId)
        {
            if (_container == null) await InitializeAsync();

            var query = new QueryDefinition(
                "SELECT c.id FROM c WHERE c.userId = @userId")
                .WithParameter("@userId", userId);

            var iterator = _container!.GetItemQueryIterator<dynamic>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var item in response)
                {
                    await _container.DeleteItemAsync<dynamic>(
                        (string)item.id, new PartitionKey(userId));
                }
            }
        }
    }
}