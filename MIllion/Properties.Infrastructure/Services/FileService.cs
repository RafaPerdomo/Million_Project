using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Properties.Infrastructure.Services
{
    public interface IFileService
    {
        Task<string> SavePhotoAsync(string base64String, string fileName);
        Task DeletePhotoAsync(string filePath);
    }

    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileService> _logger;
        private const string UploadsFolder = "Uploads/Photos";

        public FileService(IWebHostEnvironment environment, ILogger<FileService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> SavePhotoAsync(string base64String, string fileName)
        {
            try
            {
                var uploadsPath = Path.Combine(_environment.WebRootPath, UploadsFolder);
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var fileExtension = GetFileExtension(base64String);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                var base64Data = base64String.Split(',')[1];
                var bytes = Convert.FromBase64String(base64Data);
                
                await File.WriteAllBytesAsync(filePath, bytes);
                
                return $"/{UploadsFolder}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving photo");
                throw new InvalidOperationException("Error saving photo", ex);
            }
        }

        public async Task DeletePhotoAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !filePath.StartsWith($"/{UploadsFolder}/"))
                return;

            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
                if (File.Exists(fullPath))
                {
                    await Task.Run(() => File.Delete(fullPath));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo: {FilePath}", filePath);
            }
        }

        private static string GetFileExtension(string base64String)
        {
            var data = base64String.Substring(0, 5);
            return data.ToUpper() switch
            {
                "IVBOR" => ".png",
                "/9J/4" => ".jpg",
                "AAAAF" => ".mp4",
                "JVBER" => ".pdf",
                _ => ".jpg"
            };
        }
    }
}
