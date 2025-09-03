using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace properties.Api.Application.Common.DTOs
{
    public class OwnerDto
    { 
        public int? Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        [MaxLength(200)]
        public string Address { get; set; }
        [MaxLength(500)]
        public string Photo { get; set; }
        [Required]
        public DateTime Birthday { get; set; }
        public List<PropertyDto> Properties { get; set; } = new List<PropertyDto>();
    }
}