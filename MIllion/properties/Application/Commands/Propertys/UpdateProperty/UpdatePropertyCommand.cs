using MediatR;
using Microsoft.AspNetCore.Http;
using properties.Api.Application.Common.DTOs;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace properties.Api.Application.Commands.Propertys.UpdateProperty
{
    public class UpdatePropertyCommand : IRequest<ResponseDto>
    {
        [JsonIgnore]
        public int Id { get; set; }
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string Name { get; set; }
        [StringLength(200, ErrorMessage = "Address cannot be longer than 200 characters")]
        public string Address { get; set; }
        [StringLength(50, ErrorMessage = "Internal code cannot be longer than 50 characters")]
        public string CodeInternal { get; set; }
        [Range(1800, 2100, ErrorMessage = "Year must be between 1800 and 2100")]
        public int? Year { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive number")]
        public decimal? Price { get; set; }
    }
}