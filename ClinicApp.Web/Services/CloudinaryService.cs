using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;

namespace ClinicApp.Web.Services
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile file);
    }

    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrEmpty(cloudName) ||
                string.IsNullOrEmpty(apiKey) ||
                string.IsNullOrEmpty(apiSecret))
            {
                throw new InvalidOperationException("Cloudinary configuration is missing.");
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty", nameof(file));

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "clinicapp/patients"   // اسم فولدر في Cloudinary
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception($"Cloudinary upload failed: {result.Error?.Message}");

            return result.SecureUrl.ToString();  // https URL
        }
    }
}
