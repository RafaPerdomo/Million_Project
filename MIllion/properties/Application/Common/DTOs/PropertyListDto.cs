namespace properties.Api.Application.Common.DTOs
{
    public class PropertyListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public decimal Price { get; set; }
        public int Year { get; set; }
        public string CodeInternal { get; set; }
        public string OwnerName { get; set; }
        public int ImageCount { get; set; }
        public PropertyTraceDto LastTrace { get; set; }
    }
}
