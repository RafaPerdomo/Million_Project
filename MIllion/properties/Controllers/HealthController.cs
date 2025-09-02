using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Properties.Infrastructure.Persistence.Context;
using System;
using System.Threading.Tasks;
namespace properties.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private const string CacheTestKey = "_HealthCheckTestKey_";
        private readonly TimeSpan _cacheTestExpiration = TimeSpan.FromSeconds(1);

        public HealthController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }
        [HttpGet("database")]
        public async Task<IActionResult> CheckDatabase()
        {
            try
            {
                await _context.Database.OpenConnectionAsync();
                var canConnect = await _context.Database.CanConnectAsync();
                if (canConnect)
                {
                    return Ok(new
                    {
                        status = "Healthy",
                        database = _context.Database.GetDbConnection().Database,
                        dataSource = _context.Database.GetDbConnection().DataSource,
                        message = "Database connection is working"
                    });
                }
                return StatusCode(500, new { status = "Unhealthy", message = "Cannot connect to database" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "Error",
                    message = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }
        
        [HttpGet("cache")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckCache()
        {
            try
            {
                var testValue = Guid.NewGuid().ToString();
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(_cacheTestExpiration);
                
                _cache.Set(CacheTestKey, testValue, cacheOptions);

                if (!_cache.TryGetValue(CacheTestKey, out string cachedValue) || cachedValue != testValue)
                {
                    return StatusCode(500, new 
                    { 
                        status = "Unhealthy", 
                        message = "Cache read/write test failed" 
                    });
                }

                await Task.Delay(_cacheTestExpiration.Add(TimeSpan.FromMilliseconds(100)));
                if (_cache.TryGetValue(CacheTestKey, out _))
                {
                    return StatusCode(500, new 
                    { 
                        status = "Degraded", 
                        message = "Cache expiration test failed" 
                    });
                }

                return Ok(new 
                { 
                    status = "Healthy", 
                    message = "Cache is working correctly",
                    details = new 
                    {
                        type = _cache.GetType().Name,
                        testPerformed = "Read/Write and Expiration"
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "Error",
                    message = "Cache health check failed",
                    details = new 
                    {
                        error = ex.Message,
                        innerError = ex.InnerException?.Message
                    }
                });
            }
        }
    }
}