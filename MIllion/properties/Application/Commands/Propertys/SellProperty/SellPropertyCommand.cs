using MediatR;
using properties.Api.Application.Common.DTOs;
using System;
using System.ComponentModel.DataAnnotations;
namespace properties.Api.Application.Commands.Propertys.SellProperty
{
    public class SellPropertyCommand : IRequest<ResponseDto>
    {
        [Required]
        public int PropertyId { get; set; }
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Sale price must be greater than 0")]
        public decimal SalePrice { get; set; }
        [Required]
        public int NewOwnerId { get; set; }
        public OwnerDto NewOwner { get; set; }
        [Required]
        [Range(0, 100, ErrorMessage = "Tax must be between 0 and 100")]
        public decimal TaxPercentage { get; set; }
        public string Notes { get; set; } = "Property Sale";
    }
}