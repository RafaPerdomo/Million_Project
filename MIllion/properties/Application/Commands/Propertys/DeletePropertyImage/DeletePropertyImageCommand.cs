using MediatR;
namespace properties.Api.Application.Commands.Propertys.DeletePropertyImage
{
    public class DeletePropertyImageCommand : IRequest<bool>
    {
        public int ImageId { get; set; }
    }
}