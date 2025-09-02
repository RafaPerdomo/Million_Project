using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
namespace properties.Api.Application.Common.DTOs
{
    public class PropertyDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public decimal Price { get; set; }
        public string CodeInternal { get; set; }
        public int Year { get; set; }
        public OwnerDto Owner { get; set; }
        public ICollection<PropertyTraceDto> PropertyTrace { get; set; }
        public ICollection<PropertyImageDto> PropertyImages { get; set; }
    }
    public class PropertyTraceDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime? Date { get; set; }
        public decimal? Value { get; set; }
        public decimal? Tax { get; set; }
    }
    public class PropertyImageDto
    {
        public int Id { get; set; }
        public bool Enabled { get; set; }
    }
    public class PropertyImageDetailDto
    {
        public int Id { get; set; }
        public string File { get; set; }
        public bool Enabled { get; set; } = true;
        public int IdProperty { get; set; }
    }
}