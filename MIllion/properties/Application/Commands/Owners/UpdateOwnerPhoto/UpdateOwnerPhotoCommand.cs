using MediatR;
using properties.Api.Application.Common.DTOs;
using System;
using System.ComponentModel.DataAnnotations;
namespace properties.Api.Application.Commands.Owners.UpdateOwnerPhoto
{
    public class UpdateOwnerPhotoCommand : IRequest<ResponseDto>
    {
        [Required]
        public int IdOwner { get; set; }
        [Required]
        public string PhotoBase64 { get; set; }
    }
}