using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Properties.Domain.Entities;
using Properties.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using Properties.Domain.Exceptions;
using properties.Api.Application.Common.DTOs;
using Microsoft.EntityFrameworkCore;
using Properties.Infrastructure.Persistence.Context;

namespace properties.Api.Application.Commands.Propertys.UpdateProperty
{
    public class UpdatePropertyCommandHandler : IRequestHandler<UpdatePropertyCommand, ResponseDto>
    {
        private readonly IPropertyRepository _propertyRepository;
        private readonly IPropertyTraceRepository _propertyTraceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdatePropertyCommandHandler> _logger;
        private readonly ApplicationDbContext _context;

        public UpdatePropertyCommandHandler(
            IPropertyRepository propertyRepository,
            IPropertyTraceRepository propertyTraceRepository,
            IUnitOfWork unitOfWork,
            ILogger<UpdatePropertyCommandHandler> logger,
            ApplicationDbContext context)
        {
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _propertyTraceRepository = propertyTraceRepository ?? throw new ArgumentNullException(nameof(propertyTraceRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ResponseDto> Handle(UpdatePropertyCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting update process for property ID {PropertyId}", request.Id);
            
            var strategy = _context.Database.CreateExecutionStrategy();
            
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                
                try
                {
                    if (request.Id <= 0)
                    {
                        throw new ArgumentException("Invalid property ID");
                    }

                    var property = await _propertyRepository.GetByIdWithDetailsAsync(request.Id, includeInactive: false, cancellationToken);
                    if (property == null)
                    {
                        throw new PropertyNotFoundException(request.Id);
                    }

                    bool priceChanged = request.Price.HasValue && property.Price != request.Price.Value;
                    decimal? oldPrice = priceChanged ? property.Price : null;
                    
                    UpdatePropertyFields(property, request);
                    
                    if (priceChanged)
                    {
                        await HandlePriceChangeAsync(property, request.Price.Value, cancellationToken);
                    }

                    await _propertyRepository.UpdateAsync(property, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("Successfully updated property with ID {PropertyId}", property.Id);

                    return new ResponseDto
                    {
                        Success = true,
                        Message = "Property updated successfully",
                        Data = new
                        {
                            Id = property.Id,
                            OldPrice = oldPrice,
                            NewPrice = property.Price,
                            PriceChanged = priceChanged
                        }
                    };
                }
                catch (PropertyNotFoundException ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogWarning(ex, "Property not found: {PropertyId}", request.Id);
                    throw;
                }
                catch (PropertyAlreadyExistsException ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogWarning(ex, "Property with same code already exists");
                    throw;
                }
                catch (InvalidPropertyOperationException)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Error updating property with ID {PropertyId}", request.Id);
                    throw new InvalidPropertyOperationException("An error occurred while updating the property", ex);
                }
            });
        }

        private async Task HandlePriceChangeAsync(Property property, decimal newPrice, CancellationToken cancellationToken)
        {
            var trace = new PropertyTrace
            {
                PropertyId = property.Id,
                DateSale = DateTime.UtcNow,
                Name = "Price Update",
                Value = newPrice,
                Tax = 0,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _propertyTraceRepository.AddAsync(trace, cancellationToken);
            _logger.LogInformation("Created price change trace for property {PropertyId}", property.Id);
        }

        private void UpdatePropertyFields(Property property, UpdatePropertyCommand request)
        {
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                property.Name = request.Name.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Address))
            {
                property.Address = request.Address.Trim();
            }

            if (request.Price.HasValue)
            {
                property.Price = request.Price.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.CodeInternal))
            {
                property.CodeInternal = request.CodeInternal.Trim();
            }

            if (request.Year.HasValue)
            {
                property.Year = request.Year.Value;
            }
            
           

            property.UpdatedAt = DateTime.UtcNow;
        }
    }
}