using LanChat.Common.Models;
using Microsoft.Azure.Cosmos;

namespace LanChat.Server.Services;

public class CosmosDbService
{
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseName = "LanChatDb";
    private readonly string _containerName = "Messages";

    public CosmosDbService(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;
    }

    public async Task InitializeDatabaseAsync()
    {
        DatabaseResponse databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName); //error line
        Database database = databaseResponse.Database;

        await database.CreateContainerIfNotExistsAsync(
            id: _containerName,
            partitionKeyPath: "/chatRoomId",
            throughput: 400
        );
    }

    private Container GetContainer()
    {
        return _cosmosClient.GetContainer(_databaseName, _containerName);
    }

    public async Task AddMessageAsync(ChatMessage message)
    {
        var container = GetContainer();
        await container.CreateItemAsync(message, new PartitionKey(message.ChatRoomId));
    }

    public async Task DeleteMessageAsync(string messageId, string chatRoomId)
    {
        var container = GetContainer();
        await container.DeleteItemAsync<ChatMessage>(messageId, new PartitionKey(chatRoomId));
    }

    public async Task<List<ChatMessage>> GetRecentMessagesAsync(string chatRoomId)
    {
        var container = GetContainer();

        var query = new QueryDefinition("SELECT * FROM c WHERE c.chatRoomId = @roomId ORDER BY c._ts DESC")
            .WithParameter("@roomId", chatRoomId);

        var iterator = container.GetItemQueryIterator<ChatMessage>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(chatRoomId) }
        );

        var results = new List<ChatMessage>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response.ToList());
        }
        return results;
    }

    public async Task<List<ChatMessage>> GetMessagesByUserAsync(string userId)
    {
        var container = GetContainer();

        var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId")
            .WithParameter("@userId", userId);

        var iterator = container.GetItemQueryIterator<ChatMessage>(query);

        var results = new List<ChatMessage>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response.ToList());
        }
        return results;
    }

    public async Task<List<ChatMessage>> GetMessagesBySenderAsync(string sender)
    {
        var container = GetContainer();

        var query = new QueryDefinition("SELECT * FROM c WHERE c.sender = @sender")
            .WithParameter("@sender", sender);

        var iterator = container.GetItemQueryIterator<ChatMessage>(query);

        var results = new List<ChatMessage>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response.ToList());
        }
        return results;
    }
}