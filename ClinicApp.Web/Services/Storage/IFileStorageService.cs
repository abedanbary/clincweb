namespace ClinicApp.Web.Services.Storage;

public interface IFileStorageService
{
    Task<UploadedFileResult> UploadAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken = default);

    Task<string> GenerateSignedReadUrlAsync(
        string objectPath,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string objectPath,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string objectPath,
        CancellationToken cancellationToken = default);
}

public sealed class UploadedFileResult
{
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ObjectPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime UploadedAtUtc { get; set; }
}
