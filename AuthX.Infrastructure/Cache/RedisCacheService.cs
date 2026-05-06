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
        try
        {
            var bytes = await _cache.GetAsync(key);
            if (bytes == null || bytes.Length == 0) return default;
            return JsonSerializer.Deserialize<T>(bytes, _opts);
        }
        catch (Exception)
        {
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _opts);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(10)
            };
            await _cache.SetAsync(key, bytes, options);
        }
        catch (Exception)
        {
            // Redis down — silently skip
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
        }
        catch (Exception)
        {
            // Redis down — silently skip
        }
    }

    public async Task RemoveByPrefixAsync(string prefix)
    {
        try
        {
            await _cache.RemoveAsync(prefix);
        }
        catch (Exception)
        {
            // Redis down — silently skip
        }
    }
}