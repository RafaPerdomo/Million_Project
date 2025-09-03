using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using properties.Api.Application.Common.DTOs;
using properties.Api.Infrastructure.Cache;
using Properties.Domain.Entities;
using Properties.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace properties.Api.Application.Commands.Propertys.CreatePropertyImages
{
    public class CreatePropertyImagesCommandHandler : IRequestHandler<CreatePropertyImagesCommand, ResponseDto>
    {
        private readonly IPropertyImageRepository _propertyImageRepository;
        private readonly IPropertyRepository _propertyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreatePropertyImagesCommandHandler> _logger;
        private readonly CreatePropertyImagesCommandValidator _validator;
        private readonly IMemoryCache _cache;

        public CreatePropertyImagesCommandHandler(
            IPropertyImageRepository propertyImageRepository,
            IPropertyRepository propertyRepository,
            IUnitOfWork unitOfWork,
            ILogger<CreatePropertyImagesCommandHandler> logger,
            CreatePropertyImagesCommandValidator validator,
            IMemoryCache cache)
        {
            _propertyImageRepository = propertyImageRepository ?? throw new ArgumentNullException(nameof(propertyImageRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<ResponseDto> Handle(CreatePropertyImagesCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return new ResponseDto
                {
                    Success = false,
                    Message = "Validation error",
                    Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
                };
            }
            try
            {
                var propertyExists = await _propertyRepository.GetByIdAsync(request.PropertyId, cancellationToken);
                if (propertyExists == null)
                {
                    return new ResponseDto
                    {
                        Success = false,
                        Message = "The property was not found"
                    };
                }
                if (request.Images == null || !request.Images.Any())
                {
                    return new ResponseDto
                    {
                        Success = false,
                        Message = "No images were provided for upload"
                    };
                }
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                var savedImages = new List<string>();
                var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                const int maxFileSize = 5 * 1024 * 1024; 
                var propertyImages = new List<PropertyImage>();
                foreach (var image in request.Images)
                {
                    if (image == null || image.Length == 0)
                        continue;
                    var fileExtension = Path.GetExtension(image.FileName)?.ToLowerInvariant();
                    if (string.IsNullOrEmpty(fileExtension) || !validExtensions.Contains(fileExtension))
                    {
                        _logger.LogWarning($"File {image.FileName} has an invalid extension");
                        continue;
                    }
                    if (image.Length > maxFileSize)
                    {
                        _logger.LogWarning($"File {image.FileName} exceeds the maximum allowed size");
                        continue;
                    }
                    try
                    {
                        string base64Image;
                        using (var ms = new MemoryStream())
                        {
                            await image.CopyToAsync(ms);
                            var fileBytes = ms.ToArray();
                            base64Image = $"data:{image.ContentType};base64,{Convert.ToBase64String(fileBytes)}";
                        }
                        var propertyImage = new PropertyImage
                        {
                            PropertyId = request.PropertyId,
                            File = base64Image,
                            IsActive = true
                        };
                        propertyImages.Add(propertyImage);
                        savedImages.Add(propertyImage.File);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing image {image.FileName}");
                    }
                }
                if (propertyImages.Any())
                {
                    await _propertyImageRepository.AddRangeAsync(propertyImages, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                _logger.LogInformation("Uploaded {SavedCount} of {TotalCount} images for property {PropertyId}",
                    savedImages.Count, request.Images.Count, request.PropertyId);

                InvalidateCaches(propertyExists.Id, propertyExists.OwnerId);

                return new ResponseDto
                {
                    Success = true,
                    Message = $"Successfully uploaded {savedImages.Count} of {request.Images.Count} images",
                    Data = savedImages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing images");
                return new ResponseDto
                {
                    Success = false,
                    Message = "An error occurred while processing the images: " + ex.Message
                };
            }
        }

        private void InvalidateCaches(int propertyId, int? ownerId)
        {
            try
            {
                _cache.Remove(CacheKeyHelper.PropertyByIdKey(propertyId));
                
                var propertyListKeys = _cache.GetKeysByPrefix(CacheKeyHelper.AllPropertiesKeyPrefix);
                foreach (var key in propertyListKeys)
                {
                    _cache.Remove(key);
                }

                if (ownerId.HasValue)
                {
                    _cache.Remove(CacheKeyHelper.OwnerByIdKey(ownerId.Value));
                    _cache.Remove(CacheKeyHelper.AllOwnersKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating caches for property {PropertyId}", propertyId);
            }
        }
    }
}