using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace AuthX.Infrastructure.Cache;

public interface IRedisCacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
}

public class RedisCacheService : IRedisCacheService
{
    private readonly IDistributedCache _cache;
    private static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisCacheService(IDistributedCache cache) => _cache = cache;

    public async Task<T?> GetAsync<T>(string key)
    {
        var bytes = await _cache.GetAsync(key);
        if (bytes == null || bytes.Length == 0) return default;
        return JsonSerializer.Deserialize<T>(bytes, _opts);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _opts);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(10)
        };
        await _cache.SetAsync(key, bytes, options);
    }

    public async Task RemoveAsync(string key)
        => await _cache.RemoveAsync(key);

    public async Task RemoveByPrefixAsync(string prefix)
    {
        // For pattern-based removal, use StackExchange.Redis directly
        // This is a simplified version — in production inject IConnectionMultiplexer
        await _cache.RemoveAsync(prefix);
    }
}