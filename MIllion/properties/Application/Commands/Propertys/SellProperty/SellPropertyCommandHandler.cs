using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using properties.Api.Application.Common.DTOs;
using properties.Api.Infrastructure.Cache;
using Properties.Domain.Entities;
using Properties.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace properties.Api.Application.Commands.Propertys.SellProperty
{
    public class SellPropertyCommandHandler : IRequestHandler<SellPropertyCommand, ResponseDto>
    {
        private readonly IPropertyRepository _propertyRepository;
        private readonly IOwnerRepository _ownerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SellPropertyCommandHandler> _logger;
        private readonly IPropertyTraceRepository _propertyTraceRepository;
        private readonly IMemoryCache _cache;
        public SellPropertyCommandHandler(
            IPropertyRepository propertyRepository,
            IOwnerRepository ownerRepository,
            IUnitOfWork unitOfWork,
            ILogger<SellPropertyCommandHandler> logger,
            IPropertyTraceRepository propertyTraceRepository,
            IMemoryCache cache)
        {
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _ownerRepository = ownerRepository ?? throw new ArgumentNullException(nameof(ownerRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _propertyTraceRepository = propertyTraceRepository ?? throw new ArgumentNullException(nameof(propertyTraceRepository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<ResponseDto> Handle(SellPropertyCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting property sale process for property ID {PropertyId}", request.PropertyId);

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(request);
            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                _logger.LogWarning("Validation failed for SellPropertyCommand");
                return new ResponseDto
                {
                    Success = false,
                    Message = "Validation failed",
                    Errors = validationResults.Select(vr => vr.ErrorMessage).ToList()
                };
            }

                var property = await _propertyRepository.GetByIdWithDetailsAsync(request.PropertyId, includeInactive: false, cancellationToken);
                if (property == null)
                {
                    _logger.LogWarning("Property with ID {PropertyId} not found", request.PropertyId);
                    return new ResponseDto
                    {
                        Success = false,
                        Message = $"Property with ID {request.PropertyId} not found"
                    };
                }

                var newOwner = await GetOrCreateOwnerAsync(request, cancellationToken);
                if (newOwner == null)
                {
                    return new ResponseDto
                    {
                        Success = false,
                        Message = $"Failed to get or create owner with ID {request.NewOwnerId}"
                    };
                }

                if (request.NewOwner != null)
                {
                    await UpdateOwnerDetailsAsync(newOwner, request.NewOwner, cancellationToken);
                }

                var propertyTrace = await CreatePropertyTraceAsync(property, newOwner, request.SalePrice, request.TaxPercentage);

                var previousOwnerId = property.OwnerId;
                property.OwnerId = newOwner.Id;
                property.Price = request.SalePrice;

                try
                {
                    await _propertyRepository.UpdateAsync(property, cancellationToken);
                    await _propertyTraceRepository.AddAsync(propertyTrace);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    InvalidateCaches(property.Id, previousOwnerId, newOwner.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating property and trace for property ID {PropertyId}", property.Id);
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    throw;
                }

                _logger.LogInformation("Successfully transferred property {PropertyId} from owner {PreviousOwnerId} to {NewOwnerId}",
                    property.Id, previousOwnerId, newOwner.Id);

                return new ResponseDto
                {
                    Success = true,
                    Message = "Property sold successfully",
                    Data = new
                    {
                        PropertyId = property.Id,
                        PreviousOwnerId = previousOwnerId,
                        NewOwnerId = newOwner.Id,
                        request.SalePrice,
                        propertyTrace.Tax,
                        SaleDate = propertyTrace.DateSale
                    }
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing property sale for property ID {PropertyId}", request.PropertyId);
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        private async Task InvalidateCaches(int propertyId, int previousOwnerId, int newOwnerId)
        {
            try
            {
                _cache.Remove(CacheKeyHelper.PropertyByIdKey(propertyId));
                _cache.Remove(CacheKeyHelper.OwnerByIdKey(previousOwnerId));
                _cache.Remove(CacheKeyHelper.OwnerByIdKey(newOwnerId));
                _cache.Remove(CacheKeyHelper.AllOwnersKey);
                
                var propertyListKeys = _cache.GetKeysByPrefix(CacheKeyHelper.AllPropertiesKeyPrefix);
                foreach (var key in propertyListKeys)
                {
                    _cache.Remove(key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating caches for property {PropertyId}", propertyId);
            }
        }

        private async Task<Owner> GetOrCreateOwnerAsync(SellPropertyCommand request, CancellationToken cancellationToken)
        {
            var owner = await _ownerRepository.GetByIdAsync(request.NewOwnerId);
            if (owner != null) return owner;
            if (request.NewOwner == null)
            {
                _logger.LogWarning("Owner with ID {OwnerId} not found and no owner data provided", request.NewOwnerId);
                return null;
            }
            owner = new Owner
            {
                Id = request.NewOwnerId,
                Name = request.NewOwner.Name.Trim(),
                Address = request.NewOwner.Address?.Trim(),
                Photo = request.NewOwner.Photo?.Trim(),
                Birthday = request.NewOwner.Birthday
            };
            
            await _ownerRepository.AddAsync(owner);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created new owner with ID {OwnerId}", owner.Id);
            return owner;
        }
        private async Task UpdateOwnerDetailsAsync(Owner owner, OwnerDto ownerDto, CancellationToken cancellationToken)
        {
            bool updated = false;
            if (!string.IsNullOrWhiteSpace(ownerDto.Name) && owner.Name != ownerDto.Name)
            {
                owner.Name = ownerDto.Name.Trim();
                updated = true;
            }
            if (ownerDto.Address != null && owner.Address != ownerDto.Address)
            {
                owner.Address = ownerDto.Address.Trim();
                updated = true;
            }
            if (ownerDto.Photo != null && owner.Photo != ownerDto.Photo)
            {
                owner.Photo = ownerDto.Photo.Trim();
                updated = true;
            }
            if (ownerDto.Birthday != default && owner.Birthday != ownerDto.Birthday)
            {
                owner.Birthday = ownerDto.Birthday;
                updated = true;
            }
            if (updated)
            {
                await _ownerRepository.UpdateAsync(owner);
                _logger.LogInformation("Updated details for owner with ID {OwnerId}", owner.Id);
            }
        }
        private Task<PropertyTrace> CreatePropertyTraceAsync(Property property, Owner newOwner, decimal salePrice, decimal taxPercentage)
        {
            return Task.FromResult(new PropertyTrace
            {
                PropertyId = property.Id,
                DateSale = DateTime.UtcNow,
                Name = $"Sold to {newOwner.Name}",
                Value = salePrice,
                Tax = salePrice * (taxPercentage / 100m)
            });
        }
    }
}