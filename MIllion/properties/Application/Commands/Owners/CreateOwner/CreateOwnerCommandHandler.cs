using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
namespace properties.Api.Application.Commands.Owners.CreateOwner
{
    public class CreateOwnerCommandHandler : IRequestHandler<CreateOwnerCommand, ResponseDto>
    {
        private readonly ILogger<CreateOwnerCommandHandler> _logger;
        private readonly IOwnerRepository _ownerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;
        public CreateOwnerCommandHandler(
            ILogger<CreateOwnerCommandHandler> logger,
            IOwnerRepository ownerRepository,
            IUnitOfWork unitOfWork,
            IMemoryCache cache)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ownerRepository = ownerRepository ?? throw new ArgumentNullException(nameof(ownerRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }
        public async Task<ResponseDto> Handle(CreateOwnerCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting to create new owner: {OwnerName}", request.Name);
                
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(request);
                if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
                {
                    var errorMessages = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                    throw new InvalidOwnerOperationException($"Invalid owner data: {errorMessages}");
                }

                if (request.IdOwner > 0)
                {
                    var existingOwner = await _ownerRepository.GetByIdAsync(request.IdOwner);
                    if (existingOwner != null)
                    {
                        return new ResponseDto
                        {
                            Success = false,
                            Message = $"Ya existe un propietario con el ID {request.IdOwner}"
                        };
                    }
                }

                var owner = new Owner
                {
                    Id = request.IdOwner,  // Usar Id en lugar de IdOwner
                    Name = request.Name.Trim(),
                    Address = request.Address?.Trim(),
                    Birthday = request.Birthday,
                    Photo = string.Empty
                };
                
                await _ownerRepository.AddAsync(owner);
                
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Successfully created new owner with ID {OwnerId}", owner.Id);
                _cache.Remove(CacheKeyHelper.AllOwnersKey);
                _logger.LogInformation("Invalidated owner caches after creation");
                
                return new ResponseDto
                {
                    Success = true,
                    Message = "Owner created successfully",
                    Data = new OwnerDto
                    {
                        Id = owner.Id,
                        Name = owner.Name,
                        Address = owner.Address,
                        Photo = owner.Photo,
                        Birthday = owner.Birthday
                    }
                };
            }
            catch (OwnerAlreadyExistsException ex)
            {
                _logger.LogWarning(ex, "Failed to create owner - already exists: {OwnerName}", request.Name);
                throw;
            }
            catch (InvalidOwnerOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid owner operation during creation");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating owner: {OwnerName}", request.Name);
                throw new InvalidOwnerOperationException("An unexpected error occurred while creating the owner", ex);
            }
        }
    }
}