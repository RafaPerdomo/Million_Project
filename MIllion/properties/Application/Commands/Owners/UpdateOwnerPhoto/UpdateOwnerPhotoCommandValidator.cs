using FluentValidation;
using System;
using System.Linq;
namespace properties.Api.Application.Commands.Owners.UpdateOwnerPhoto
{
    public class UpdateOwnerPhotoCommandValidator : AbstractValidator<UpdateOwnerPhotoCommand>
    {
        private const int MaxFileSizeInBytes = 5 * 1024 * 1024;
        private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/gif" };
        public UpdateOwnerPhotoCommandValidator()
        {
            RuleFor(x => x.IdOwner)
                .GreaterThan(0)
                .WithMessage("Owner ID must be greater than zero.");
            RuleFor(x => x.PhotoBase64)
                .NotEmpty()
                .WithMessage("A base64 encoded image is required.")
                .Must(BeAValidBase64Image)
                .WithMessage("The provided string is not a valid base64 encoded image.")
                .Must(BeAValidImageSize)
                .WithMessage("The image size must not exceed 5MB.")
                .Must(BeAValidImageType)
                .WithMessage("The image must be a valid type (JPEG, PNG, GIF).");
        }
        private bool BeAValidBase64Image(string base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String) || !base64String.StartsWith("data:image/"))
                return false;
            try
            {
                var base64Data = base64String.Split(',').Last().Trim();
                Convert.FromBase64String(base64Data);
                return true;
            }
            catch
            {
                return false;
            }
        }
        private bool BeAValidImageSize(string base64String)
        {
            try
            {
                var base64Data = base64String.Split(',').Last().Trim();
                var bytes = Convert.FromBase64String(base64Data);
                return bytes.Length <= MaxFileSizeInBytes;
            }
            catch
            {
                return false;
            }
        }
        private bool BeAValidImageType(string base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String) || !base64String.StartsWith("data:"))
                return false;
            var mimeType = base64String.Split(';')[0].Split(':')[1];
            return AllowedMimeTypes.Contains(mimeType);
        }
    }
}