using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace properties.Api.Infrastructure.Cache
{
    public static class CacheServiceCollectionExtensions
    {
        public static IServiceCollection AddCachingServices(this IServiceCollection services)
        {
            services.AddMemoryCache();

            services.Configure<CacheSettings>(options =>
            {
                options.DefaultSlidingExpiration = TimeSpan.FromMinutes(10);
                options.DefaultAbsoluteExpiration = TimeSpan.FromHours(1);
            });

            services.AddScoped(provider =>
                provider.GetRequiredService<IOptions<CacheSettings>>().Value);

            return services;
        }
    }
}
