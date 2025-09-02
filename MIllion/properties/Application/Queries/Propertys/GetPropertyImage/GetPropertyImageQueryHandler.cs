using MediatR;
using Microsoft.Extensions.Logging;
using properties.Api.Application.Common.DTOs;
using Properties.Domain.Entities;
using Properties.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace properties.Api.Application.Queries.Propertys.GetPropertyImage
{
    public class GetPropertyImageQueryHandler : IRequestHandler<GetPropertyImageQuery, PropertyImageDetailDto>
    {
        private readonly ILogger<GetPropertyImageQueryHandler> _logger;
        private readonly IPropertyImageRepository _propertyImageRepository;
        public GetPropertyImageQueryHandler(
            ILogger<GetPropertyImageQueryHandler> logger,
            IPropertyImageRepository propertyImageRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _propertyImageRepository = propertyImageRepository ?? throw new ArgumentNullException(nameof(propertyImageRepository));
        }
        public async Task<PropertyImageDetailDto> Handle(GetPropertyImageQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Retrieving property image with ID {ImageId}", request.ImageId);
            try
            {
                var image = await _propertyImageRepository.GetByIdAsync(request.ImageId, cancellationToken);
                if (image == null)
                {
                    _logger.LogWarning("Property image with ID {ImageId} not found", request.ImageId);
                    return null;
                }
                _logger.LogInformation("Successfully retrieved property image with ID {ImageId}", request.ImageId);
                return MapToDto(image);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving property image with ID {ImageId}", request.ImageId);
                throw; 
            }
        }
        private static PropertyImageDetailDto MapToDto(PropertyImage image)
        {
            if (image == null) return null;
            return new PropertyImageDetailDto
            {
                Id = image.Id,
                IdProperty = image.PropertyId,
                File = image.File,
                Enabled = image.IsActive
            };
        }
    }
}