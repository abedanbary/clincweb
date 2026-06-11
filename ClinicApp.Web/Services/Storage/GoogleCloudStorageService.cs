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

    // Extensions accepted regardless of content type (browsers send generic types for 3D files).
    private static readonly HashSet<string> ThreeDExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".stl", ".obj", ".ply", ".glb", ".gltf" };

    // All extensions this service will accept.
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".stl", ".obj", ".ply", ".glb", ".gltf" };

    // Content types required for non-3D files.
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/webp",
        "model/stl",
        "model/obj",
        "model/ply",
        "model/gltf-binary",
        "model/gltf+json",
        "application/octet-stream", // generic browser fallback for binary files
        "text/plain"                // some browsers send .stl/.obj as text/plain
    };

    public GoogleCloudStorageService()
    {
        var credentialsJson = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_JSON")
            ?? throw new InvalidOperationException(
                "GOOGLE_CREDENTIALS_JSON environment variable is not set.");

        _bucketName = Environment.GetEnvironmentVariable("GOOGLE_BUCKET_NAME")
            ?? throw new InvalidOperationException(
                "GOOGLE_BUCKET_NAME environment variable is not set.");

        var maxMb = int.TryParse(Environment.GetEnvironmentVariable("MAX_UPLOAD_FILE_SIZE_MB"), out var mb) ? mb : 50;
        _maxFileSizeBytes = maxMb * 1024L * 1024L;

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

        var extension = (Path.GetExtension(file.FileName) ?? string.Empty).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException(
                $"File extension '{extension}' is not allowed.");

        var browserContentType = (file.ContentType ?? string.Empty).Split(';')[0].Trim().ToLowerInvariant();

        // For non-3D files, the browser content type must also be valid.
        if (!ThreeDExtensions.Contains(extension) && !AllowedContentTypes.Contains(browserContentType))
            throw new ArgumentException(
                $"File type '{browserContentType}' is not allowed for extension '{extension}'.");

        // Use canonical MIME type for 3D files when the browser sends a generic type.
        var effectiveContentType = ThreeDExtensions.Contains(extension)
            ? (PatientFileHelper.CanonicalMimeForThreeD(extension) ?? browserContentType)
            : browserContentType;

        var safeFolder = NormalizeFolder(folder);
        var guid = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        var storedFileName = $"{guid}{extension}";
        var objectPath = $"{safeFolder}/{now:yyyy}/{now:MM}/{now:dd}/{storedFileName}";

        using var stream = file.OpenReadStream();
        await _storageClient.UploadObjectAsync(
            _bucketName,
            objectPath,
            effectiveContentType,
            stream,
            cancellationToken: cancellationToken);

        return new UploadedFileResult
        {
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = storedFileName,
            ObjectPath = objectPath,
            ContentType = effectiveContentType,
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

        var template = UrlSigner.RequestTemplate
            .FromBucket(_bucketName)
            .WithObjectName(objectPath)
            .WithHttpMethod(HttpMethod.Get);

        var options = UrlSigner.Options.FromDuration(expiration ?? TimeSpan.FromHours(1));

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

    public async Task StreamToAsync(
        string objectPath,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectPath))
            throw new ArgumentException("Object path must not be empty.", nameof(objectPath));

        await _storageClient.DownloadObjectAsync(
            _bucketName,
            objectPath,
            destination,
            cancellationToken: cancellationToken);
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
