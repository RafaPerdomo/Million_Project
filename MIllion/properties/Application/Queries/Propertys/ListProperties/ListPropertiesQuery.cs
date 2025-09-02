using MediatR;
using properties.Api.Application.Common.DTOs;

namespace properties.Api.Application.Queries.Propertys.ListProperties
{
    public class ListPropertiesQuery : IRequest<PropertiesListResponseDto>
    {
        public string Name { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? Year { get; set; }
        public int? OwnerId { get; set; }
        public int? PropertyTypeId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}