using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace properties.Infrastructure.HealthChecks
{
    public class CacheHealthCheck : IHealthCheck
    {
        private readonly IMemoryCache _cache;
        private readonly string _testKey = "_HealthCheckTestKey_";
        private readonly TimeSpan _testExpiration = TimeSpan.FromSeconds(1);

        public CacheHealthCheck(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var testValue = Guid.NewGuid().ToString();
                _cache.Set(_testKey, testValue, _testExpiration);

                if (!_cache.TryGetValue(_testKey, out string cachedValue) || cachedValue != testValue)
                {
                    return new HealthCheckResult(
                        context.Registration.FailureStatus, 
                        "Cache read/write test failed");
                }

                await Task.Delay(_testExpiration.Add(TimeSpan.FromMilliseconds(100)), cancellationToken);
                if (_cache.TryGetValue(_testKey, out _))
                {
                    return new HealthCheckResult(
                        context.Registration.FailureStatus, 
                        "Cache expiration test failed");
                }

                return HealthCheckResult.Healthy("Cache is working correctly");
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(
                    context.Registration.FailureStatus, 
                    "Cache health check failed", 
                    ex);
            }
        }
    }
}
