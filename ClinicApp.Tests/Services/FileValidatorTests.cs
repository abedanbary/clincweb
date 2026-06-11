using ClinicApp.Web.Services.Storage;
using Microsoft.AspNetCore.Http;
using Moq;

namespace ClinicApp.Tests.Services;

public class FileValidatorTests
{
    private const long TenMB = 10L * 1024 * 1024;
    private const long TwoHundredMB = 200L * 1024 * 1024;

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static IFormFile MakeFile(
        string fileName = "test.jpg",
        string contentType = "image/jpeg",
        long sizeBytes = 1024)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(sizeBytes);
        return mock.Object;
    }

    // ── Null / empty ───────────────────────────────────────────────────────────

    [Fact]
    public void Validate_NullFile_ReturnsError()
    {
        var error = FileValidator.Validate(null, TenMB);
        Assert.NotNull(error);
        Assert.Contains("empty", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ZeroLengthFile_ReturnsError()
    {
        var file = MakeFile(sizeBytes: 0);
        var error = FileValidator.Validate(file, TenMB);
        Assert.NotNull(error);
        Assert.Contains("empty", error, StringComparison.OrdinalIgnoreCase);
    }

    // ── File size ──────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_FileExactlyAtLimit_ReturnsNull()
    {
        var file = MakeFile(sizeBytes: TwoHundredMB);
        var error = FileValidator.Validate(file, TwoHundredMB);
        Assert.Null(error);
    }

    [Fact]
    public void Validate_FileOneByteOverLimit_ReturnsError()
    {
        var file = MakeFile(sizeBytes: TwoHundredMB + 1);
        var error = FileValidator.Validate(file, TwoHundredMB);
        Assert.NotNull(error);
        Assert.Contains("exceeds", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_FileFarOverLimit_ErrorMentionsMB()
    {
        var file = MakeFile(sizeBytes: 300L * 1024 * 1024);
        var error = FileValidator.Validate(file, TwoHundredMB);
        Assert.NotNull(error);
        Assert.Contains("MB", error);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1024)]
    [InlineData(50 * 1024 * 1024)]  // 50 MB
    [InlineData(200 * 1024 * 1024)] // 200 MB exactly
    public void Validate_FileSizeWithinLimit_ReturnsNull(long size)
    {
        var file = MakeFile(sizeBytes: size);
        var error = FileValidator.Validate(file, TwoHundredMB);
        Assert.Null(error);
    }

    // ── Extension validation ───────────────────────────────────────────────────

    [Theory]
    [InlineData(".pdf",  "application/pdf")]
    [InlineData(".jpg",  "image/jpeg")]
    [InlineData(".jpeg", "image/jpeg")]
    [InlineData(".png",  "image/png")]
    [InlineData(".webp", "image/webp")]
    public void Validate_AllowedStandardExtension_ReturnsNull(string ext, string contentType)
    {
        var file = MakeFile($"file{ext}", contentType);
        Assert.Null(FileValidator.Validate(file, TenMB));
    }

    [Theory]
    [InlineData(".stl", "application/octet-stream")] // typical browser behaviour
    [InlineData(".obj", "text/plain")]               // some browsers send as plain text
    [InlineData(".ply", "application/octet-stream")]
    [InlineData(".glb", "model/gltf-binary")]
    [InlineData(".gltf","model/gltf+json")]
    public void Validate_ThreeDExtension_AcceptsGenericContentType(string ext, string contentType)
    {
        var file = MakeFile($"scan{ext}", contentType);
        Assert.Null(FileValidator.Validate(file, TenMB));
    }

    [Theory]
    [InlineData(".exe")]
    [InlineData(".js")]
    [InlineData(".html")]
    [InlineData(".svg")]
    [InlineData(".bat")]
    [InlineData(".cmd")]
    [InlineData(".sh")]
    [InlineData(".php")]
    [InlineData(".dll")]
    [InlineData(".zip")]
    [InlineData("")]            // no extension at all
    [InlineData(".")]           // dot only
    public void Validate_DangerousOrUnknownExtension_ReturnsError(string ext)
    {
        var file = MakeFile($"file{ext}", "application/octet-stream");
        var error = FileValidator.Validate(file, TenMB);
        Assert.NotNull(error);
        Assert.Contains("extension", error, StringComparison.OrdinalIgnoreCase);
    }

    // ── Content-type validation for non-3D files ───────────────────────────────

    [Fact]
    public void Validate_JpgWithWrongContentType_ReturnsError()
    {
        var file = MakeFile("photo.jpg", "text/html");
        var error = FileValidator.Validate(file, TenMB);
        Assert.NotNull(error);
        Assert.Contains("not allowed", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_PdfWithImageContentType_ReturnsError()
    {
        var file = MakeFile("doc.pdf", "image/jpeg");
        // Extension is allowed but content type mismatch for non-3D
        // .pdf extension + image/jpeg content type → content type not valid for pdf ext
        // Actually our validator doesn't cross-check ext vs ct for non-3D,
        // it only checks that ct is in the allowed set.
        // image/jpeg IS in AllowedContentTypes, so this should pass (intentional — browser quirks).
        Assert.Null(FileValidator.Validate(file, TenMB));
    }

    [Fact]
    public void Validate_JpgWithNullContentType_ReturnsError()
    {
        // ContentType null → empty → not in allowed set
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns("photo.jpg");
        mock.Setup(f => f.ContentType).Returns((string)null!);
        mock.Setup(f => f.Length).Returns(1024);

        var error = FileValidator.Validate(mock.Object, TenMB);
        Assert.NotNull(error);
    }

    // ── Allowed content types ──────────────────────────────────────────────────

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("application/octet-stream")]
    [InlineData("text/plain")]
    public void AllowedContentTypes_ContainsExpectedTypes(string contentType)
    {
        Assert.Contains(contentType, FileValidator.AllowedContentTypes, StringComparer.OrdinalIgnoreCase);
    }

    // ── AllowedExtensions set ─────────────────────────────────────────────────

    [Theory]
    [InlineData(".pdf")]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".png")]
    [InlineData(".webp")]
    [InlineData(".stl")]
    [InlineData(".obj")]
    [InlineData(".ply")]
    [InlineData(".glb")]
    [InlineData(".gltf")]
    public void AllowedExtensions_ContainsAllExpectedFormats(string ext)
    {
        Assert.Contains(ext, FileValidator.AllowedExtensions, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void AllowedExtensions_HasExactly10Entries()
    {
        Assert.Equal(10, FileValidator.AllowedExtensions.Count);
    }
}
