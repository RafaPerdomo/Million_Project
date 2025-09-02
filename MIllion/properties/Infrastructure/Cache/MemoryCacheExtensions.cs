using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace properties.Api.Infrastructure.Cache
{
    public static class MemoryCacheExtensions
    {
        private static readonly FieldInfo _entriesCollectionField;
        private static readonly Type _cacheEntryType;
        private static readonly PropertyInfo _keyProperty;

        static MemoryCacheExtensions()
        {
            var cacheType = typeof(MemoryCache);
            _entriesCollectionField = cacheType.GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance) ??
                                    cacheType.GetField("_coherentState", BindingFlags.NonPublic | BindingFlags.Instance);

            _cacheEntryType = cacheType.GetNestedType("CacheEntry", BindingFlags.NonPublic) ??
                             cacheType.Assembly.GetType("Microsoft.Extensions.Caching.Memory.CacheEntry");

            _keyProperty = _cacheEntryType?.GetProperty("Key");
        }

        public static IEnumerable<string> GetKeysByPrefix(this IMemoryCache cache, string prefix)
        {
            if (cache == null) 
                throw new ArgumentNullException(nameof(cache));
                
            if (string.IsNullOrEmpty(prefix)) 
                throw new ArgumentNullException(nameof(prefix));

            if (_entriesCollectionField == null || _cacheEntryType == null || _keyProperty == null)
            {
                return Array.Empty<string>();
            }

            try
            {
                var entries = _entriesCollectionField.GetValue(cache);
                if (entries == null) return Array.Empty<string>();

                var keys = new List<string>();
                
                if (entries is IDictionary cacheItems)
                {
                    foreach (DictionaryEntry item in cacheItems)
                    {
                        var key = item.Key.ToString();
                        if (key != null && key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            keys.Add(key);
                        }
                    }
                }
                else if (entries.GetType().GetProperty("Keys") is PropertyInfo keysProp)
                {
                    if (keysProp.GetValue(entries) is ICollection<string> keyCollection)
                    {
                        keys.AddRange(keyCollection.Where(k => k != null && k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
                    }
                }

                return keys;
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public static int RemoveByPrefix(this IMemoryCache cache, string prefix)
        {
            if (cache == null) 
                throw new ArgumentNullException(nameof(cache));
                
            var keys = GetKeysByPrefix(cache, prefix).ToList();
            foreach (var key in keys)
            {
                cache.Remove(key);
            }
            return keys.Count;
        }

        public static TItem GetOrCreateWithOptions<TItem>(
            this IMemoryCache cache,
            string key,
            Func<ICacheEntry, TItem> factory,
            MemoryCacheEntryOptions options)
        {
            if (!cache.TryGetValue(key, out var result))
            {
                using var entry = cache.CreateEntry(key);
                entry.SetOptions(options);
                result = factory(entry);
                entry.Value = result;
            }
            return (TItem)result;
        }
    }
}
