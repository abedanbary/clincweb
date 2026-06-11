using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using System.Text;

namespace ClinicApp.Web.Services.Storage;

public sealed class GoogleCloudStorageService : IFileStorageService
{
    private readonly StorageClient _storageClient;
    private readonly UrlSigner _urlSigner;
    private readonly string _bucketName;
    private readonly long _maxFileSizeBytes;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "image/jpeg", "image/png", "image/webp"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".webp"
    };

    public GoogleCloudStorageService()
    {
        var credentialsJson = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_JSON")
            ?? throw new InvalidOperationException(
                "GOOGLE_CREDENTIALS_JSON environment variable is not set. " +
                "Provide the service account JSON as the value of this variable.");

        _bucketName = Environment.GetEnvironmentVariable("GOOGLE_BUCKET_NAME")
            ?? throw new InvalidOperationException(
                "GOOGLE_BUCKET_NAME environment variable is not set. " +
                "Provide the name of your Google Cloud Storage bucket.");

        var maxMb = int.TryParse(Environment.GetEnvironmentVariable("MAX_UPLOAD_FILE_SIZE_MB"), out var mb) ? mb : 10;
        _maxFileSizeBytes = maxMb * 1024L * 1024L;

        // Suppress obsolete warnings — these APIs still function correctly in v4.x
#pragma warning disable CS0618
        var credential = GoogleCredential.FromJson(credentialsJson);
        _storageClient = StorageClient.Create(credential);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(credentialsJson));
        _urlSigner = UrlSigner.FromServiceAccountData(stream);
#pragma warning restore CS0618
    }

    public async Task<UploadedFileResult> UploadAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or not provided.", nameof(file));

        if (file.Length > _maxFileSizeBytes)
            throw new ArgumentException(
                $"File size ({file.Length / 1024 / 1024} MB) exceeds the maximum allowed size of {_maxFileSizeBytes / 1024 / 1024} MB.");

        var contentType = (file.ContentType ?? string.Empty).Split(';')[0].Trim().ToLowerInvariant();
        if (!AllowedContentTypes.Contains(contentType))
            throw new ArgumentException(
                $"File type '{contentType}' is not allowed. Allowed types: application/pdf, image/jpeg, image/png, image/webp.");

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException(
                $"File extension '{extension}' is not allowed. Allowed extensions: .pdf, .jpg, .jpeg, .png, .webp.");

        var safeFolder = NormalizeFolder(folder);
        var guid = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        var storedFileName = $"{guid}{extension}";
        var objectPath = $"{safeFolder}/{now:yyyy}/{now:MM}/{now:dd}/{storedFileName}";

        using var stream = file.OpenReadStream();
        await _storageClient.UploadObjectAsync(
            _bucketName,
            objectPath,
            contentType,
            stream,
            cancellationToken: cancellationToken);

        return new UploadedFileResult
        {
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = storedFileName,
            ObjectPath = objectPath,
            ContentType = contentType,
            Size = file.Length,
            UploadedAtUtc = now
        };
    }

    public async Task<string> GenerateSignedReadUrlAsync(
        string objectPath,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectPath))
            throw new ArgumentException("Object path must not be empty.", nameof(objectPath));

        var duration = expiration ?? TimeSpan.FromHours(1);

        var template = UrlSigner.RequestTemplate
            .FromBucket(_bucketName)
            .WithObjectName(objectPath)
            .WithHttpMethod(HttpMethod.Get);

        var options = UrlSigner.Options.FromDuration(duration);

        return await _urlSigner.SignAsync(template, options, cancellationToken);
    }

    public async Task DeleteAsync(string objectPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectPath))
            throw new ArgumentException("Object path must not be empty.", nameof(objectPath));

        await _storageClient.DeleteObjectAsync(_bucketName, objectPath, cancellationToken: cancellationToken);
    }

    public async Task<bool> ExistsAsync(string objectPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectPath))
            return false;

        try
        {
            await _storageClient.GetObjectAsync(_bucketName, objectPath, cancellationToken: cancellationToken);
            return true;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private static string NormalizeFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
            return "uploads";

        var segments = folder
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(seg => string.Concat(seg.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')))
            .Where(s => !string.IsNullOrEmpty(s))
            .Take(5)
            .ToArray();

        return segments.Length == 0 ? "uploads" : string.Join("/", segments);
    }
}
