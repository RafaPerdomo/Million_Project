using System;
using System.Collections.Generic;

namespace properties.Api.Application.Common.DTOs
{
    public class PropertiesListResponseDto
    {
        public IEnumerable<PropertyListDto> Properties { get; set; } = new List<PropertyListDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }
}