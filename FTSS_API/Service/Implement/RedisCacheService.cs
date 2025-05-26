using System.Text.Json;
using StackExchange.Redis;

public class RedisCacheService
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            return value.HasValue ? JsonSerializer.Deserialize<T>(value!) : default;
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError($"Redis connection failed: {ex.Message}");
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, expiry);
            // Lưu key vào Set để quản lý
            await _db.SetAddAsync("ProductCacheKeys", key);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError($"Redis connection failed: {ex.Message}");
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _db.KeyDeleteAsync(key);
            await _db.SetRemoveAsync("ProductCacheKeys", key); // RedisValue is fine here
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError($"Redis connection failed: {ex.Message}");
        }
    }

    public async Task ClearProductCacheAsync()
    {
        try
        {
            var keys = await _db.SetMembersAsync("ProductCacheKeys");
            foreach (var key in keys)
            {
                // Convert RedisValue to string
                await _db.KeyDeleteAsync(key.ToString());
            }
            await _db.KeyDeleteAsync("ProductCacheKeys");
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError($"Redis connection failed: {ex.Message}");
        }
    }
}