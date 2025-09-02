using MediatR;
using properties.Api.Application.Common.DTOs;

namespace properties.Api.Application.Queries.Propertys.GetPropertyImage
{
    public class GetPropertyImageQuery : IRequest<PropertyImageDetailDto>
    {
        public int ImageId { get; set; }
    }
}