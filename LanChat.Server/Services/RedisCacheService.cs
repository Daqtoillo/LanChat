using StackExchange.Redis;
using System.Text.Json;

namespace LanChat.Server.Services
{
    public class RedisCacheService
    {
        private readonly IDatabase _db;
    
        public RedisCacheService(IConnectionMultiplexer multiplexer)
        {
            _db = multiplexer.GetDatabase();
        }

        public async Task<T?> GetCacheDataAsync<T>(string key)
        {
            var jsonData = await _db.StringGetAsync(key);

            if (jsonData.IsNullOrEmpty)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(jsonData!);
        }

        public async Task SetCacheDataAsync<T>(string key, T data, TimeSpan expirationTime)
        {
            var jsonData = JsonSerializer.Serialize(data);

            await _db.StringSetAsync(key, jsonData, expirationTime);
        }
    }
}
