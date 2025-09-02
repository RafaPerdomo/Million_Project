using Swashbuckle.AspNetCore.Filters;
namespace properties.Controllers.Examples
{
    public class SellPropertyCommandExample : IExamplesProvider<object>
    {
        public object GetExamples()
        {
            return new
            {
                newOwnerId = 2,
                salePrice = 250000.00,
                taxPercentage = 5.0,
                newOwner = new
                {
                    name = "pedro Perez",
                    address = "Calle Falsa 123",
                    photo = "pedro-perez.jpg",
                    birthday = "1980-01-15"
                }
            };
        }
    }
}