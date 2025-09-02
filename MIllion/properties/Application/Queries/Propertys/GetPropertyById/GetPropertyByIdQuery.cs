using MediatR;
using properties.Api.Application.Common.DTOs;

namespace properties.Api.Application.Queries.Propertys.GetPropertyById
{
    public class GetPropertyByIdQuery : IRequest<ResponseDto>
    {
        public int Id { get; set; }
    }
}
