using FluentValidation;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
namespace properties.Api.Application.Commands.Propertys.CreatePropertyImages
{
    public class CreatePropertyImagesCommandValidator : AbstractValidator<CreatePropertyImagesCommand>
    {
        private const int MaxFileSizeInBytes = 1 * 1024 * 1024;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        public CreatePropertyImagesCommandValidator()
        {
            RuleFor(x => x.PropertyId)
                .GreaterThan(0)
                .WithMessage("Property ID must be greater than zero.");
            RuleFor(x => x.Images)
                .NotEmpty()
                .WithMessage("At least one image must be provided.")
                .Must(images => images != null && images.All(IsValidImage))
                .WithMessage("One or more files are not valid images or exceed the maximum size of 1MB.");
        }
        private bool IsValidImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;
            if (file.Length > MaxFileSizeInBytes)
                return false;
            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            return !string.IsNullOrEmpty(extension) && AllowedExtensions.Contains(extension);
        }
    }
}