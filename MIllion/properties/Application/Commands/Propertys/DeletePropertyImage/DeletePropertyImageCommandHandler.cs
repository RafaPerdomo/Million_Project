using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using properties.Api.Infrastructure.Cache;
using Properties.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace properties.Api.Application.Commands.Propertys.DeletePropertyImage
{
    public class DeletePropertyImageCommandHandler : IRequestHandler<DeletePropertyImageCommand, bool>
    {
        private readonly IPropertyImageRepository _propertyImageRepository;
        private readonly IPropertyRepository _propertyRepository;
        private readonly ILogger<DeletePropertyImageCommandHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;

        public DeletePropertyImageCommandHandler(
            IPropertyImageRepository propertyImageRepository,
            IPropertyRepository propertyRepository,
            ILogger<DeletePropertyImageCommandHandler> logger,
            IUnitOfWork unitOfWork,
            IMemoryCache cache)
        {
            _propertyImageRepository = propertyImageRepository ?? throw new ArgumentNullException(nameof(propertyImageRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<bool> Handle(DeletePropertyImageCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Attempting to delete property image with ID {ImageId}", request.ImageId);
                var image = await _propertyImageRepository.GetByIdWithPropertyAsync(request.ImageId, cancellationToken);
                if (image == null)
                {
                    _logger.LogWarning("Property image with ID {ImageId} not found", request.ImageId);
                    return false;
                }

                var propertyId = image.PropertyId;
                var result = await _propertyImageRepository.DeleteAsync(image.Id, cancellationToken);
                if (!result)
                {
                    _logger.LogError("Failed to delete property image with ID {ImageId}", request.ImageId);
                    return false;
                }
                await _unitOfWork.CommitAsync(cancellationToken);
                _logger.LogInformation("Successfully deleted property image with ID {ImageId}", request.ImageId);

                var property = await _propertyRepository.GetByIdAsync(propertyId, cancellationToken);
                if (property != null)
                {
                    InvalidateCaches(property.Id, property.OwnerId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting property image with ID {ImageId}", request.ImageId);
                await _unitOfWork.RollbackAsync(cancellationToken);
                return false;
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