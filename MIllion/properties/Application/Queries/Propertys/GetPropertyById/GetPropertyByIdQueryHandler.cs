using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using properties.Api.Infrastructure.Cache;
using properties.Api.Application.Common.DTOs;
using Properties.Domain.Entities;
using Properties.Domain.Interfaces;
using Properties.Domain.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using properties.Api.Infrastructure.Cache;
using System.Linq;

namespace properties.Api.Application.Queries.Propertys.GetPropertyById
{
    public class GetPropertyByIdQueryHandler : IRequestHandler<GetPropertyByIdQuery, ResponseDto>
    {
        private readonly IPropertyRepository _propertyRepository;
        private readonly ILogger<GetPropertyByIdQueryHandler> _logger;
        private readonly IMemoryCache _cache;
        private readonly CacheSettings _cacheSettings;

        public GetPropertyByIdQueryHandler(
            IPropertyRepository propertyRepository,
            ILogger<GetPropertyByIdQueryHandler> logger,
            IMemoryCache cache,
            IOptions<CacheSettings> cacheSettings)
        {
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _cacheSettings = cacheSettings?.Value ?? throw new ArgumentNullException(nameof(cacheSettings));
        }

        public async Task<ResponseDto> Handle(GetPropertyByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeyHelper.PropertyByIdKey(request.Id);
            _logger.LogInformation("Checking cache for property with ID: {PropertyId}", request.Id);

            if (_cache.TryGetValue(cacheKey, out ResponseDto cachedResponse) && cachedResponse != null)
            {
                _logger.LogInformation("Retrieved property with ID {PropertyId} from cache", request.Id);
                return cachedResponse;
            }

            _logger.LogInformation("Cache miss - fetching property with ID: {PropertyId} from database", request.Id);

            try
            {
                var property = await _propertyRepository.GetByIdWithDetailsAsync(request.Id, includeInactive: false, cancellationToken);
                
                if (property == null)
                {
                    throw new PropertyNotFoundException(request.Id);
                }

                var response = new ResponseDto
                {
                    Success = true,
                    Message = "Property retrieved successfully",
                    Data = MapToDto(property)
                };

                var cacheOptions = _cacheSettings.CreateCacheOptions(
                    slidingExpiration: TimeSpan.FromMinutes(15),
                    absoluteExpiration: TimeSpan.FromHours(2));
                _cache.Set(cacheKey, response, cacheOptions);
                _logger.LogInformation("Cached property with ID: {PropertyId}", request.Id);

                return response;
            }
            catch (PropertyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Property not found: {PropertyId}", request.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching property with ID: {PropertyId}", request.Id);
                throw new InvalidPropertyOperationException("An error occurred while retrieving the property", ex);
            }
        }

        private object MapToDto(Property property)
        {
            return new
            {
                property.Id,
                property.Name,
                property.Address,
                property.Price,
                property.CodeInternal,
                property.Year,
                Owner = property.Owner != null ? new
                {
                    property.Owner.Id,
                    property.Owner.Name,
                    property.Owner.Address,
                    property.Owner.Photo,
                    property.Owner.Birthday
                } : null,
                Images = property.PropertyImages?.Select(pi => new 
                {
                    pi.Id,
                    pi.File,
                    pi.IsActive,
                    pi.CreatedAt
                }) ?? Enumerable.Empty<object>(),
                Traces = property.PropertyTraces?.Select(pt => new
                {
                    pt.Id,
                    pt.Name,
                    pt.Value,
                    pt.Tax,
                    pt.DateSale
                }).OrderByDescending(pt => pt.DateSale) ?? Enumerable.Empty<object>()
            };
        }
    }
}
