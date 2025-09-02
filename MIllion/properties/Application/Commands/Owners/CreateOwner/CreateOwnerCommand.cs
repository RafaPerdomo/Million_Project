using MediatR;
using properties.Api.Application.Common.DTOs;
using System;
using System.ComponentModel.DataAnnotations;
namespace properties.Api.Application.Commands.Owners.CreateOwner
{
    public class CreateOwnerCommand : IRequest<ResponseDto>
    {
        [Required]
        public int IdOwner { get; set; }
        [Required]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string Name { get; set; }
        [StringLength(200, ErrorMessage = "Address cannot be longer than 200 characters")]
        public string Address { get; set; }
        [Required]
        public DateTime Birthday { get; set; }
    }
}