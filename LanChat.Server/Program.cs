using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LanChat.Server.Hubs;
using LanChat.Server.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Azure;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

string? keyVaultUri = builder.Configuration["KeyVaultUri"];

if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}

builder.Services.AddSingleton<KeyVaultCryptoService>();

var redisConnectionString = builder.Configuration["RedisConnection"];

builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
    ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddSingleton<RedisCacheService>();

builder.Services.AddSignalR();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//var cosmosConnectionString = builder.Configuration["CosmosDbConnection"];

var cosmosConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

builder.Services.AddSingleton(sp =>
{
    return new CosmosClient(cosmosConnectionString);
});

builder.Services.AddSingleton<CosmosDbService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowAnyOrigin());
});

builder.Services.AddSingleton<BlobService>();

builder.Services.AddAzureClients(clientBuilder =>
{
    var storageConnectionString = builder.Configuration["StorageConnection:ConnectionString"];

    clientBuilder.AddBlobServiceClient(storageConnectionString!);
    clientBuilder.AddQueueServiceClient(storageConnectionString);
    clientBuilder.AddTableServiceClient(storageConnectionString);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var blobServiceClient = scope.ServiceProvider.GetRequiredService<BlobServiceClient>();
    var startupContainerClient = blobServiceClient.GetBlobContainerClient("images");
    await startupContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);

    var cosmosDbService = scope.ServiceProvider.GetRequiredService<CosmosDbService>();
    await cosmosDbService.InitializeDatabaseAsync();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.MapHub<ChatHub>("/chatHub");

app.Run();
