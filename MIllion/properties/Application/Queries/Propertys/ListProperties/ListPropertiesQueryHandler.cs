using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using properties.Api.Application.Common.DTOs;
using properties.Api.Infrastructure.Cache;
using Properties.Domain.Entities;
using Properties.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
namespace properties.Api.Application.Queries.Propertys.ListProperties
{
    public class ListPropertiesQueryHandler : IRequestHandler<ListPropertiesQuery, PropertiesListResponseDto>
    {
        private readonly ILogger<ListPropertiesQueryHandler> _logger;
        private readonly IPropertyRepository _propertyRepository;
        private readonly IMemoryCache _cache;
        private readonly CacheSettings _cacheSettings;
        public ListPropertiesQueryHandler(
            ILogger<ListPropertiesQueryHandler> logger,
            IPropertyRepository propertyRepository,
            IMemoryCache cache,
            IOptions<CacheSettings> cacheSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _cacheSettings = cacheSettings?.Value ?? throw new ArgumentNullException(nameof(cacheSettings));
        }
        public async Task<PropertiesListResponseDto> Handle(ListPropertiesQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = GenerateCacheKey(request);
            _logger.LogDebug("Checking cache for properties with key: {CacheKey}", cacheKey);

            if (_cache.TryGetValue(cacheKey, out PropertiesListResponseDto cachedResponse) && cachedResponse != null)
            {
                _logger.LogDebug("Cache hit for properties with key: {CacheKey}", cacheKey);
                return cachedResponse;
            }

            _logger.LogInformation("Cache miss - retrieving properties with filters: {Filters}", request);
            
            try
            {
                var (propertyDtos, totalCount) = await GetFilteredPropertiesProjectionAsync(request, cancellationToken);
                
                var response = new PropertiesListResponseDto
                {
                    Properties = propertyDtos,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };

                var cacheOptions = _cacheSettings.CreateCacheOptions(
                    slidingExpiration: TimeSpan.FromMinutes(5),
                    absoluteExpiration: TimeSpan.FromHours(1));
                _cache.Set(cacheKey, response, cacheOptions);
                
                    
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving properties with filters: {Filters}", request);
                throw new ApplicationException("An error occurred while retrieving properties. Please try again later.", ex);
            }
        }

        private string GenerateCacheKey(ListPropertiesQuery request)
        {
            if (string.IsNullOrEmpty(request.Name) && 
                !request.MinPrice.HasValue && 
                !request.MaxPrice.HasValue && 
                !request.Year.HasValue && 
                !request.OwnerId.HasValue)
            {
                return CacheKeyHelper.PropertiesListKey();
            }
            
            var cacheKey = CacheKeyHelper.GenerateListCacheKey(
                request.PageNumber,
                request.PageSize,
                request.Name,
                request.MinPrice,
                request.MaxPrice,
                request.Year,
                request.OwnerId);
                
            if (request.PropertyTypeId.HasValue)
            {
                cacheKey += $"_Type_{request.PropertyTypeId}";
            }
                
            return cacheKey;
        }
        private async Task<(IEnumerable<PropertyListDto> Properties, int TotalCount)> GetFilteredPropertiesProjectionAsync(
            ListPropertiesQuery request, 
            CancellationToken cancellationToken)
        {
            var baseQuery = _propertyRepository.GetQueryable()
                .AsNoTracking()
                .Where(p => p.IsActive);
                
            var filteredQuery = ApplyFilters(baseQuery, request);
            var totalCount = await filteredQuery.CountAsync(cancellationToken);

            var properties = await filteredQuery
                .OrderBy(p => p.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new PropertyListDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Address = p.Address,
                    Price = p.Price,
                    Year = p.Year,
                    CodeInternal = p.CodeInternal,
                    OwnerName = p.Owner.Name,
                    ImageCount = p.PropertyImages.Count,
                    LastTrace = p.PropertyTraces
                        .OrderByDescending(t => t.DateSale)
                        .Select(t => new PropertyTraceDto
                        {
                            Date = t.DateSale,
                            Value = t.Value,
                            Tax = t.Tax
                        })
                        .FirstOrDefault()
                })
                .AsSplitQuery()
                .TagWith($"PropertyListQuery_Page{request.PageNumber}_Size{request.PageSize}")
                .ToListAsync(cancellationToken);
                
            return (properties, totalCount);
        }

        private IQueryable<Property> ApplyFilters(IQueryable<Property> query, ListPropertiesQuery request)
        {
            if (!string.IsNullOrEmpty(request.Name))
            {
                query = query.Where(p => p.Name.Contains(request.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (request.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= request.MinPrice.Value);
            }

            if (request.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= request.MaxPrice.Value);
            }

            if (request.Year.HasValue)
            {
                query = query.Where(p => p.Year == request.Year.Value);
            }

            if (request.OwnerId.HasValue)
            {
                query = query.Where(p => p.OwnerId == request.OwnerId.Value);
            }


            return query;
        }

        private static List<PropertyDto> MapToPropertyDtos(IEnumerable<Property> properties)
        {
            return properties.Select(p => new PropertyDto
            {
                Id = p.Id,
                Name = p.Name,
                PropertyImages = p.PropertyImages?.Select(pi => new PropertyImageDto
                {
                    Id = pi.Id,
                    Enabled = pi.IsActive
                }).ToList() ?? new List<PropertyImageDto>(),
                Address = p.Address,
                Price = p.Price,
                CodeInternal = p.CodeInternal,
                Year = p.Year,
                Owner = p.Owner == null ? null : new OwnerDto
                {
                    Id = p.Owner.Id,
                    Name = p.Owner.Name,
                    Address = p.Owner.Address,
                    Photo = p.Owner.Photo,
                    Birthday = p.Owner.Birthday
                },
                PropertyTrace = p.PropertyTraces?.Select(pt => new PropertyTraceDto
                {
                    Id = pt.Id,
                    Name = pt.Name,
                    Date = pt.DateSale,
                    Value = pt.Value,
                    Tax = pt.Tax
                }).ToList() ?? new List<PropertyTraceDto>()
            }).ToList();
        }
    }
}