using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using properties.Api.Infrastructure.Cache;
using properties.Api.Application.Common.DTOs;
using Properties.Domain.Entities;
using Properties.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Properties.Domain.Exceptions;

namespace properties.Api.Application.Queries.Owners.GetOwnerById
{
    public class GetOwnerByIdQueryHandler : IRequestHandler<GetOwnerByIdQuery, OwnerDto>
    {
        private readonly ILogger<GetOwnerByIdQueryHandler> _logger;
        private readonly IOwnerRepository _ownerRepository;
        private readonly IMemoryCache _cache;
        private readonly CacheSettings _cacheSettings;
        public GetOwnerByIdQueryHandler(
            ILogger<GetOwnerByIdQueryHandler> logger,
            IOwnerRepository ownerRepository,
            IMemoryCache cache,
            IOptions<CacheSettings> cacheSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ownerRepository = ownerRepository ?? throw new ArgumentNullException(nameof(ownerRepository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _cacheSettings = cacheSettings?.Value ?? throw new ArgumentNullException(nameof(cacheSettings));
        }
        public async Task<OwnerDto> Handle(GetOwnerByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeyHelper.OwnerByIdKey(request.Id);
            _logger.LogInformation("Checking cache for owner with ID {OwnerId}", request.Id);
            
            if (_cache.TryGetValue(cacheKey, out OwnerDto cachedOwner) && cachedOwner != null)
            {
                _logger.LogInformation("Retrieved owner with ID {OwnerId} from cache", request.Id);
                return cachedOwner;
            }

            _logger.LogInformation("Cache miss - retrieving owner with ID {OwnerId} from database", request.Id);
            
            try
            {
                var owner = await _ownerRepository.GetByIdWithPropertiesAsync(request.Id);
                if (owner == null)
                {
                    throw new OwnerNotFoundException(request.Id);
                }

                var ownerDto = new OwnerDto
                {
                    Id = owner.Id,
                    Name = owner.Name,
                    Address = owner.Address,
                    Photo = owner.Photo,
                    Birthday = owner.Birthday,
                    Properties = owner.Properties?.Select(p => new PropertyDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Address = p.Address,
                        Price = p.Price,
                        CodeInternal = p.CodeInternal,
                        Year = p.Year,
                        PropertyTrace = p.PropertyTraces?.Select(t => new PropertyTraceDto
                        {
                            Id = t.Id,
                            Name = t.Name,
                            Date = t.DateSale,
                            Value = t.Value,
                            Tax = t.Tax
                        }).ToList() ?? new List<PropertyTraceDto>(),
                        PropertyImages = p.PropertyImages?.Select(pi => new PropertyImageDto
                        {
                            Id = pi.Id,
                            Enabled = pi.IsActive
                        }).ToList() ?? new List<PropertyImageDto>()
                    }).ToList() ?? new List<PropertyDto>()
                };

                var cacheOptions = _cacheSettings.CreateCacheOptions(
                    slidingExpiration: TimeSpan.FromMinutes(15),
                    absoluteExpiration: TimeSpan.FromHours(2));
                _cache.Set(cacheKey, ownerDto, cacheOptions);
                _logger.LogInformation("Cached owner with ID {OwnerId}", request.Id);

                return ownerDto;
            }
            catch (OwnerNotFoundException ex)
            {
                _logger.LogWarning(ex, "Owner not found: {OwnerId}", request.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving owner with ID {OwnerId}", request.Id);
                throw new InvalidOwnerOperationException("An error occurred while retrieving the owner", ex);
            }
        }
    }
}