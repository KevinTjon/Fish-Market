using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class QueryCache
{
    private static QueryCache _instance;
    public static QueryCache Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new QueryCache();
            }
            return _instance;
        }
    }

    private class CacheEntry
    {
        public object Data { get; set; }
        public DateTime ExpiryTime { get; set; }
    }

    private Dictionary<string, CacheEntry> cache = new Dictionary<string, CacheEntry>();
    private readonly object lockObject = new object();

    // Default cache duration of 5 minutes
    private readonly TimeSpan defaultCacheDuration = TimeSpan.FromMinutes(5);

    public void Set<T>(string key, T data, TimeSpan? duration = null)
    {
        lock (lockObject)
        {
            cache[key] = new CacheEntry
            {
                Data = data,
                ExpiryTime = DateTime.UtcNow.Add(duration ?? defaultCacheDuration)
            };
        }
    }

    public bool TryGet<T>(string key, out T data)
    {
        lock (lockObject)
        {
            data = default;
            if (cache.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow < entry.ExpiryTime)
                {
                    data = (T)entry.Data;
                    return true;
                }
                else
                {
                    // Remove expired entry
                    cache.Remove(key);
                }
            }
            return false;
        }
    }

    public void Remove(string key)
    {
        lock (lockObject)
        {
            cache.Remove(key);
        }
    }

    public void Clear()
    {
        lock (lockObject)
        {
            cache.Clear();
        }
    }

    public void CleanExpiredEntries()
    {
        lock (lockObject)
        {
            var expiredKeys = cache
                .Where(kvp => DateTime.UtcNow >= kvp.Value.ExpiryTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                cache.Remove(key);
            }
        }
    }

    // Helper method to generate cache keys
    public static string GenerateKey(string baseKey, params object[] parameters)
    {
        if (parameters == null || parameters.Length == 0)
            return baseKey;

        return $"{baseKey}:{string.Join(":", parameters.Select(p => p?.ToString() ?? "null"))}";
    }
} 