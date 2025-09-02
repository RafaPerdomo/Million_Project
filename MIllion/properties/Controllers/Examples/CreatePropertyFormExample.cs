using Swashbuckle.AspNetCore.Filters;
namespace properties.Controllers.Examples
{
    public class CreatePropertyCommandExample : IExamplesProvider<object>
    {
        public object GetExamples()
        {
            return new
            {
                name = "Casa de lujo en zona exclusiva",
                address = "Calle Principal #123, Colonia Las Lomas",
                price = 1500000.00,
                codeInternal = "PROP-2023-001",
                year = 2023,
                idOwner = 1,
                owner = new
                {
                    name = "Juan Perez",
                    address = "calle 40N # 742",
                    photo = "juan-perez.jpg",
                    birthday = "1985-05-15"
                }
            };
        }
    }
}