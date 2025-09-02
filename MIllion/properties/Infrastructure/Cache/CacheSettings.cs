using Microsoft.Extensions.Caching.Memory;
using System;

namespace properties.Api.Infrastructure.Cache
{
    public class CacheSettings
    {
        public TimeSpan DefaultSlidingExpiration { get; set; } = TimeSpan.FromMinutes(10);
        public TimeSpan DefaultAbsoluteExpiration { get; set; } = TimeSpan.FromHours(1);

        public MemoryCacheEntryOptions DefaultCacheOptions => new MemoryCacheEntryOptions()
            .SetSlidingExpiration(DefaultSlidingExpiration)
            .SetAbsoluteExpiration(DefaultAbsoluteExpiration);

        public MemoryCacheEntryOptions CreateCacheOptions(
            TimeSpan? slidingExpiration = null,
            TimeSpan? absoluteExpiration = null)
        {
            var options = new MemoryCacheEntryOptions();

            if (slidingExpiration.HasValue)
                options.SetSlidingExpiration(slidingExpiration.Value);
            else
                options.SetSlidingExpiration(DefaultSlidingExpiration);

            if (absoluteExpiration.HasValue)
                options.SetAbsoluteExpiration(absoluteExpiration.Value);
            else
                options.SetAbsoluteExpiration(DefaultAbsoluteExpiration);

            return options;
        }
    }
}
