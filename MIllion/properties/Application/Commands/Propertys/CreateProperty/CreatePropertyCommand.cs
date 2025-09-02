using MediatR;
using properties.Api.Application.Common.DTOs;
using System.ComponentModel.DataAnnotations;
namespace properties.Api.Application.Commands.Propertys.CreateProperty
{
    public class CreatePropertyCommand : IRequest<ResponseDto>
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }
        [Required, MaxLength(200)]
        public string Address { get; set; }
        [Required]
        public decimal Price { get; set; }
        [Required, MaxLength(50)]
        public string CodeInternal { get; set; }
        [Required]
        public int Year { get; set; }
        [Required]
        public int IdOwner { get; set; }
        public OwnerDto Owner { get; set; }
    }
}