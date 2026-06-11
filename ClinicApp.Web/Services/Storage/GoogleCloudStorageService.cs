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

    public GoogleCloudStorageService()
    {
        var credentialsJson = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_JSON")
            ?? throw new InvalidOperationException(
                "GOOGLE_CREDENTIALS_JSON environment variable is not set.");

        _bucketName = Environment.GetEnvironmentVariable("GOOGLE_BUCKET_NAME")
            ?? throw new InvalidOperationException(
                "GOOGLE_BUCKET_NAME environment variable is not set.");

        var maxMb = int.TryParse(Environment.GetEnvironmentVariable("MAX_UPLOAD_FILE_SIZE_MB"), out var mb) ? mb : 200;
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
        var error = FileValidator.Validate(file, _maxFileSizeBytes);
        if (error != null)
            throw new ArgumentException(error);

        var extension = (Path.GetExtension(file.FileName) ?? string.Empty).ToLowerInvariant();
        var browserContentType = (file.ContentType ?? string.Empty).Split(';')[0].Trim().ToLowerInvariant();

        // Use canonical MIME type for 3D files when the browser sends a generic type.
        var effectiveContentType = FileValidator.ThreeDExtensions.Contains(extension)
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
