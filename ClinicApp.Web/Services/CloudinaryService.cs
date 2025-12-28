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
  
    // جرّب الطريقتين
    var cloudName1 = configuration["Cloudinary:CloudName"];
    var cloudName2 = configuration["CLOUDINARY__CLOUDNAME"];
    
    // استخدم اللي موجود
    var cloudName = cloudName1 ?? cloudName2;
    var apiKey = configuration["Cloudinary:ApiKey"] ?? configuration["CLOUDINARY__APIKEY"];
    var apiSecret = configuration["Cloudinary:ApiSecret"] ?? configuration["CLOUDINARY__APISECRET"];

  

    try
    {
        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _isConfigured = true;
        Console.WriteLine("✅ Cloudinary configured successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error initializing Cloudinary: {ex.Message}");
        _cloudinary = null;
        _isConfigured = false;
    }
}
        public async Task<string> UploadImageAsync(IFormFile file)
        {
            Console.WriteLine($"=== UploadImageAsync called ===");
            Console.WriteLine($"IsConfigured: {_isConfigured}");
            Console.WriteLine($"File is null: {file == null}");
            Console.WriteLine($"File length: {file?.Length ?? 0}");

            if (!_isConfigured || _cloudinary == null)
            {
                Console.WriteLine("⚠️ Cloudinary not configured, returning null");
                return null;
            }

            if (file == null || file.Length == 0)
            {
                Console.WriteLine("❌ File is empty or null");
                throw new ArgumentException("File is empty", nameof(file));
            }

            try
            {
                Console.WriteLine($"📤 Uploading file: {file.FileName} ({file.Length} bytes)");
                
                await using var stream = file.OpenReadStream();

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "clinicapp/patients"
                };

                Console.WriteLine("🔄 Calling Cloudinary API...");
                var result = await _cloudinary.UploadAsync(uploadParams);
                
                Console.WriteLine($"📥 Cloudinary response status: {result.StatusCode}");
                Console.WriteLine($"📥 Cloudinary URL: {result.SecureUrl}");

                if (result.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine($"❌ Upload failed: {result.Error?.Message}");
                    throw new Exception($"Cloudinary upload failed: {result.Error?.Message}");
                }

                Console.WriteLine($"✅ Upload successful: {result.SecureUrl}");
                return result.SecureUrl.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception during upload: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}