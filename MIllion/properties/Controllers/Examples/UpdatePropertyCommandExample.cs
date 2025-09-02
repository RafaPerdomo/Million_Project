using Swashbuckle.AspNetCore.Filters;
using properties.Api.Application.Commands.Propertys.UpdateProperty;
namespace properties.Controllers.Examples
{
    public class UpdatePropertyCommandExample : IExamplesProvider<UpdatePropertyCommand>
    {
        public UpdatePropertyCommand GetExamples()
        {
            return new UpdatePropertyCommand
            {
                Name = "Updated Beach House",
                Address = "456 Ocean View Road, Malibu",
                Year = 2023,
                Price = 350000.00m
            };
        }
    }
}