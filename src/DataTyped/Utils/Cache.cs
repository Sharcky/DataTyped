using Microsoft.Extensions.Caching.Memory;

namespace DataTyped.Utils;

public static class Cache
{
    private static readonly MemoryCache MemoryCache = new MemoryCache(new MemoryCacheOptions { });

    public static Task<T?> Get<T>(string key, Func<Task<T>> factory) => MemoryCache.GetOrCreateAsync(key, async item => await factory());

    public static void Set<T>(string key, T value) => MemoryCache.Set(key, value);
}
