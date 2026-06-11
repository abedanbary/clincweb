namespace ClinicApp.Web.Services.Storage;

/// <summary>
/// Pure static validation logic for uploaded files. Decoupled from GCS so it can be unit-tested without credentials.
/// </summary>
public static class FileValidator
{
    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".stl", ".obj", ".ply", ".glb", ".gltf" };

    public static readonly HashSet<string> ThreeDExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".stl", ".obj", ".ply", ".glb", ".gltf" };

    // Content types that are valid for standard (non-3D) files.
    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
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
        "application/octet-stream",
        "text/plain"
    };

    /// <summary>
    /// Validates a file against size, extension, and content-type rules.
    /// Returns null on success; an error message string on failure.
    /// Throws nothing — callers decide how to handle the message.
    /// </summary>
    public static string? Validate(IFormFile? file, long maxFileSizeBytes)
    {
        if (file == null || file.Length == 0)
            return "File is empty or not provided.";

        if (file.Length > maxFileSizeBytes)
            return $"File size ({file.Length / 1024 / 1024} MB) exceeds the maximum allowed size of {maxFileSizeBytes / 1024 / 1024} MB.";

        var extension = (Path.GetExtension(file.FileName) ?? string.Empty).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return $"File extension '{extension}' is not allowed. Allowed: {string.Join(", ", AllowedExtensions)}.";

        var contentType = (file.ContentType ?? string.Empty).Split(';')[0].Trim().ToLowerInvariant();

        // 3D files often arrive as application/octet-stream or text/plain — validate by extension only.
        if (ThreeDExtensions.Contains(extension))
            return null;

        // For all other files, content type must be explicitly allowed.
        if (!AllowedContentTypes.Contains(contentType))
            return $"File type '{contentType}' is not allowed for extension '{extension}'.";

        return null;
    }
}
