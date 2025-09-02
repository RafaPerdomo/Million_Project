using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using properties.Api.Infrastructure.Cache;
using Properties.Domain.Entities;
using Properties.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Properties.Domain.Exceptions;
using properties.Api.Application.Common.DTOs;
using properties.Api.Infrastructure.Cache;
namespace properties.Api.Application.Commands.Propertys.CreateProperty
{
    public class CreatePropertyCommandHandler : IRequestHandler<CreatePropertyCommand, ResponseDto>
    {
        private readonly IPropertyRepository _propertyRepository;
        private readonly IOwnerRepository _ownerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CreatePropertyCommandHandler> _logger;
        public CreatePropertyCommandHandler(
            IPropertyRepository propertyRepository,
            IOwnerRepository ownerRepository,
            IUnitOfWork unitOfWork,
            ILogger<CreatePropertyCommandHandler> logger,
            IMemoryCache cache)
        {
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _ownerRepository = ownerRepository ?? throw new ArgumentNullException(nameof(ownerRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }
        public async Task<ResponseDto> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting to create new property for owner ID: {OwnerId}", request.IdOwner);
            
            try
            {
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(request);
                if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
                {
                    var errorMessages = string.Join(", ", validationResults.Select(vr => vr.ErrorMessage));
                    throw new InvalidPropertyOperationException($"Invalid property data: {errorMessages}");
                }

                var owner = await _ownerRepository.GetByIdAsync(request.IdOwner);
                if (owner == null)
                {
                    if (request.Owner == null)
                    {
                        return new ResponseDto
                        {
                            Success = false,
                            Message = $"Owner with ID {request.IdOwner} not found and no owner data provided to create a new one"
                        };
                    }
                    owner = new Owner
                    {
                        Name = request.Owner.Name.Trim(),
                        Address = request.Owner.Address?.Trim(),
                        Photo = request.Owner.Photo?.Trim(),
                        Birthday = request.Owner.Birthday
                    };
                    await _ownerRepository.AddAsync(owner);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Created new owner with ID {OwnerId}", owner.Id);
                }
                if (request.Owner != null)
                {
                    if (!string.IsNullOrWhiteSpace(request.Owner.Name))
                        owner.Name = request.Owner.Name.Trim();
                    if (request.Owner.Address != null)
                        owner.Address = request.Owner.Address.Trim();
                    if (request.Owner.Photo != null)
                        owner.Photo = request.Owner.Photo.Trim();
                    if (request.Owner.Birthday != default)
                        owner.Birthday = request.Owner.Birthday;
                    await _ownerRepository.UpdateAsync(owner);
                    _logger.LogInformation("Updated details for owner with ID {OwnerId}", owner.Id);
                }
                var existingProperty = await _propertyRepository.GetByCodeInternalAsync(request.CodeInternal);
                if (existingProperty != null)
                {
                    throw new PropertyAlreadyExistsException("code", request.CodeInternal);
                }
                var property = new Property
                {
                    Name = request.Name,
                    Address = request.Address,
                    Price = request.Price,
                    CodeInternal = request.CodeInternal,
                    Year = request.Year,
                    OwnerId = request.IdOwner
                };
                await _propertyRepository.AddAsync(property);
                var propertyTrace = new PropertyTrace
                {
                    Name = "Property Created",
                    DateSale = DateTime.UtcNow,
                    Value = request.Price,
                    Tax = 0,
                    PropertyId = property.Id
                };
                await _propertyRepository.AddTraceAsync(propertyTrace);
                await _unitOfWork.CommitAsync(cancellationToken);

                _logger.LogInformation("Successfully created property with ID: {PropertyId}", property.Id);

                // Invalidate all relevant caches
                // 1. Property caches
                var propertyCacheKey = CacheKeyHelper.PropertyByIdKey(property.Id);
                _cache.Remove(propertyCacheKey);
                _logger.LogInformation("Invalidated cache for property ID: {PropertyId}", property.Id);

                // 2. Properties list cache
                var propertiesListKey = CacheKeyHelper.PropertiesListKey();
                _cache.Remove(propertiesListKey);
                _logger.LogInformation("Invalidated properties list cache");
                
                // 3. Owner caches if a new owner was created
                if (owner != null && owner.Id > 0)
                {
                    var ownerCacheKey = CacheKeyHelper.OwnerByIdKey(owner.Id);
                    _cache.Remove(ownerCacheKey);
                    _cache.Remove(CacheKeyHelper.AllOwnersKey);
                    _logger.LogInformation("Invalidated caches for owner ID: {OwnerId}", owner.Id);
                }

                return new ResponseDto
                {
                    Success = true,
                    Message = "Property created successfully",
                    Data = new { Id = property.Id }
                };
            }
            catch (PropertyAlreadyExistsException ex)
            {
                _logger.LogWarning(ex, "Failed to create property - already exists: {Code}", request.CodeInternal);
                throw;
            }
            catch (OwnerNotFoundException ex)
            {
                _logger.LogWarning(ex, "Owner not found while creating property");
                throw new InvalidPropertyOperationException("The specified owner does not exist", ex);
            }
            catch (InvalidPropertyOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating property");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating property for owner ID: {OwnerId}", request.IdOwner);
                throw new InvalidPropertyOperationException("An unexpected error occurred while creating the property", ex);
            }
        }
    }
}