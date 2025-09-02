using MediatR;
using Microsoft.AspNetCore.Http;
using properties.Api.Application.Common.DTOs;
using System.Collections.Generic;
namespace properties.Api.Application.Commands.Propertys.CreatePropertyImages
{
    public class CreatePropertyImagesCommand : IRequest<ResponseDto>
    {
        public int PropertyId { get; set; }
        public List<IFormFile> Images { get; set; } = new List<IFormFile>();
    }
}