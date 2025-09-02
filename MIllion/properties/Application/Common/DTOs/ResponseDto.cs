using System.Collections.Generic;
namespace properties.Api.Application.Common.DTOs
{
    public class ResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}