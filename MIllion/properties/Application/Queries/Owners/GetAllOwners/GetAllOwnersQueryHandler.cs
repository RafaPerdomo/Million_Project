using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using properties.Api.Application.Common.DTOs;
using properties.Api.Infrastructure.Cache;
using Properties.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace properties.Api.Application.Queries.Owners.GetAllOwners;
public class GetAllOwnersQueryHandler : IRequestHandler<GetAllOwnersQuery, ResponseDto>
{
    private readonly ILogger<GetAllOwnersQueryHandler> _logger;
    private readonly IOwnerRepository _ownerRepository;
    private readonly IMemoryCache _cache;
    private readonly CacheSettings _cacheSettings;
    public GetAllOwnersQueryHandler(
        ILogger<GetAllOwnersQueryHandler> logger,
        IOwnerRepository ownerRepository,
        IMemoryCache cache,
        IOptions<CacheSettings> cacheSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ownerRepository = ownerRepository ?? throw new ArgumentNullException(nameof(ownerRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _cacheSettings = cacheSettings?.Value ?? throw new ArgumentNullException(nameof(cacheSettings));
    }
    public async Task<ResponseDto> Handle(GetAllOwnersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking cache for all owners");
        
        if (_cache.TryGetValue(CacheKeyHelper.AllOwnersKey, out ResponseDto cachedResponse) && cachedResponse != null)
        {
            _logger.LogInformation("Retrieved owners from cache");
            return cachedResponse;
        }

        _logger.LogInformation("Cache miss - retrieving all owners from database");
        try
        {
            var owners = await _ownerRepository.GetAllWithPropertiesAsync();
            var ownerDtos = owners.Select(owner => new OwnerDto
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
                    Year = p.Year
                }).ToList() ?? new List<PropertyDto>()
            }).ToList();

            var response = new ResponseDto
            {
                Success = true,
                Message = ownerDtos.Any() 
                    ? "Owners retrieved successfully" 
                    : "No owners found",
                Data = ownerDtos
            };

            var cacheOptions = _cacheSettings.CreateCacheOptions();
            _cache.Set(CacheKeyHelper.AllOwnersKey, response, cacheOptions);
            _logger.LogInformation("Cached owners data");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all owners: {ErrorMessage}", ex.Message);
            return new ResponseDto
            {
                Success = false,
                Message = "An error occurred while retrieving owners",
                Errors = new List<string> { ex.Message }
            };
        }
    }
    }