using MediatR;
using properties.Api.Application.Common.DTOs;
using System;
namespace properties.Api.Application.Queries.Owners.GetOwnerById
{
    public class GetOwnerByIdQuery : IRequest<OwnerDto>
    {
        public int Id { get; set; }
    }
}