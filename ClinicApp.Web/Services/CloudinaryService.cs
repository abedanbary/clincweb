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
        private readonly Cloudinary? _cloudinary;
        private readonly bool _isConfigured;

        public CloudinaryService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"] 
                           ?? configuration["CLOUDINARY__CLOUDNAME"];
            var apiKey = configuration["Cloudinary:ApiKey"] 
                        ?? configuration["CLOUDINARY__APIKEY"];
            var apiSecret = configuration["Cloudinary:ApiSecret"] 
                           ?? configuration["CLOUDINARY__APISECRET"];

            if (string.IsNullOrEmpty(cloudName) ||
                string.IsNullOrEmpty(apiKey) ||
                string.IsNullOrEmpty(apiSecret))
            {
                Console.WriteLine("⚠️ Warning: Cloudinary not configured, image uploads disabled");
                _cloudinary = null;
                _isConfigured = false;
                return;
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _isConfigured = true;
            Console.WriteLine("✅ Cloudinary configured successfully");
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (!_isConfigured || _cloudinary == null)
            {
                Console.WriteLine("⚠️ Cloudinary not configured, returning null");
                return null; // or return a default image path
            }

            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty", nameof(file));

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "clinicapp/patients"
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception($"Cloudinary upload failed: {result.Error?.Message}");

            return result.SecureUrl.ToString();
        }
    }
}