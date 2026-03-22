using LanChat.Common.Models;
using Microsoft.Azure.Cosmos;

namespace LanChat.Server.Services
{
    public class CosmosDbService
    {
        private readonly Container _container;

        public CosmosDbService(CosmosClient cosmosClient)
        {
            _container = cosmosClient.GetContainer("LanChatDb", "Messages");
        }

        public async Task AddMessageAsync(ChatMessage message)
        {
            await _container.CreateItemAsync(message, new PartitionKey(message.Sender));
        }

        public async Task<List<ChatMessage>> GetRecentMessagesAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c ORDER BY c.SentAt DESC OFFSET 0 LIMIT 50");

            var iterator = _container.GetItemQueryIterator<ChatMessage>(query);
            var results = new List<ChatMessage>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            results.Reverse();
            return results;
        }

        public async Task DeleteMessageAsync(string messageId, string sender)
        {
            await _container.DeleteItemAsync<ChatMessage>(messageId, new PartitionKey(sender));
        }

        public async Task<List<ChatMessage>> GetMessagesByUserAsync(string username)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.Sender = @user ORDER BY c.SentAt DESC")
                .WithParameter("@user", username);

            var iterator = _container.GetItemQueryIterator<ChatMessage>(
                query,
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(username) });

            var results = new List<ChatMessage>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.ToList());
            }
            return results;
        }
    }
}
