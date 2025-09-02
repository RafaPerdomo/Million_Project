using MediatR;
using properties.Api.Application.Common.DTOs;
using System.Collections.Generic;

namespace properties.Api.Application.Queries.Owners.GetAllOwners
{
    public class GetAllOwnersQuery : IRequest<ResponseDto>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}