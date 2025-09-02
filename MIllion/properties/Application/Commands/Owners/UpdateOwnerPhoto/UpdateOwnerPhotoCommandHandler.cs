using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using properties.Api.Infrastructure.Cache;
using Properties.Domain.Entities;
using Properties.Domain.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Properties.Domain.Exceptions;
using properties.Api.Application.Common.DTOs;

namespace properties.Api.Application.Commands.Owners.UpdateOwnerPhoto
{
    public class UpdateOwnerPhotoCommandHandler : IRequestHandler<UpdateOwnerPhotoCommand, ResponseDto>
    {
        private readonly ILogger<UpdateOwnerPhotoCommandHandler> _logger;
        private readonly IOwnerRepository _ownerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;
        private readonly UpdateOwnerPhotoCommandValidator _validator;

        public UpdateOwnerPhotoCommandHandler(
            ILogger<UpdateOwnerPhotoCommandHandler> logger,
            IOwnerRepository ownerRepository,
            IUnitOfWork unitOfWork,
            UpdateOwnerPhotoCommandValidator validator,
            IMemoryCache cache)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ownerRepository = ownerRepository ?? throw new ArgumentNullException(nameof(ownerRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<ResponseDto> Handle(UpdateOwnerPhotoCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting to update photo for owner ID: {OwnerId}", request.IdOwner);

                if (string.IsNullOrWhiteSpace(request.PhotoBase64) ||
                    !request.PhotoBase64.StartsWith("data:image/"))
                {
                    throw new InvalidOwnerOperationException("Invalid base64 image format. Must start with 'data:image/'");
                }

                string[] parts = request.PhotoBase64.Split(',');
                if (parts.Length < 2)
                {
                    throw new InvalidOwnerOperationException("Invalid photo format");
                }

                try
                {
                    var buffer = Convert.FromBase64String(parts[1]);
                    if (buffer.Length > 5 * 1024 * 1024)
                    {
                        throw new InvalidOwnerOperationException("Image size must be less than 5MB");
                    }
                }
                catch (FormatException)
                {
                    throw new InvalidOwnerOperationException("Invalid base64 string format");
                }

                var owner = await _ownerRepository.GetByIdAsync(request.IdOwner);
                if (owner == null)
                {
                    throw new OwnerNotFoundException(request.IdOwner);
                }

                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errorMessages = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    throw new InvalidOwnerOperationException($"Validation failed: {errorMessages}");
                }

                // Store the base64 string directly in the database
                owner.Photo = request.PhotoBase64;
                await _ownerRepository.UpdateAsync(owner);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _cache.Remove(CacheKeyHelper.AllOwnersKey);
                _cache.Remove(CacheKeyHelper.OwnerByIdKey(owner.Id));
                _logger.LogInformation("Invalidated owner caches after photo update for owner ID: {OwnerId}", owner.Id);

                
                _logger.LogInformation("Successfully updated photo for owner ID: {OwnerId}", owner.Id);
                
                return new ResponseDto
                {
                    Success = true,
                    Message = "Owner photo updated successfully"
                };
            }
            catch (OwnerNotFoundException ex)
            {
                _logger.LogWarning(ex, "Owner not found while updating photo: {OwnerId}", request.IdOwner);
                throw;
            }
            catch (InvalidOwnerOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating owner photo");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating photo for owner ID: {OwnerId}", request.IdOwner);
                throw new InvalidOwnerOperationException("An error occurred while updating the owner photo", ex);
            }
        }
    }
}