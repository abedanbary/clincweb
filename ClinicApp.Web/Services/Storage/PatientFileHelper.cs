using ClinicApp.Web.Models;

namespace ClinicApp.Web.Services.Storage;

public static class PatientFileHelper
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp" };

    private static readonly HashSet<string> ThreeDExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".stl", ".obj", ".ply", ".glb", ".gltf" };

    public static bool IsImage(string extension) => ImageExtensions.Contains(extension);

    public static bool IsPdf(string extension) =>
        extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    public static bool IsThreeDModel(string extension) => ThreeDExtensions.Contains(extension);

    public static string GetCategorySlug(PatientFileCategory category) => category switch
    {
        PatientFileCategory.IntraoralPhoto  => "intraoral-photo",
        PatientFileCategory.PanoramicXray   => "panoramic-xray",
        PatientFileCategory.BitewingXray    => "bitewing-xray",
        PatientFileCategory.PeriapicalXray  => "periapical-xray",
        PatientFileCategory.CbctScan        => "cbct-scan",
        PatientFileCategory.IntraoralScan   => "intraoral-scan",
        PatientFileCategory.MedicalReport   => "medical-report",
        PatientFileCategory.Prescription    => "prescription",
        PatientFileCategory.Invoice         => "invoice",
        _                                   => "other"
    };

    public static string GetDisplayName(PatientFileCategory category) => category switch
    {
        PatientFileCategory.IntraoralPhoto  => "Intraoral Photo",
        PatientFileCategory.PanoramicXray   => "Panoramic X-ray",
        PatientFileCategory.BitewingXray    => "Bitewing X-ray",
        PatientFileCategory.PeriapicalXray  => "Periapical X-ray",
        PatientFileCategory.CbctScan        => "CBCT Scan",
        PatientFileCategory.IntraoralScan   => "Intraoral Scan",
        PatientFileCategory.MedicalReport   => "Medical Report",
        PatientFileCategory.Prescription    => "Prescription",
        PatientFileCategory.Invoice         => "Invoice",
        _                                   => "Other"
    };

    public static string GetHumanReadableFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / 1024.0 / 1024.0:F1} MB";
    }

    // Maps a 3D model extension to the canonical MIME type when the browser sends a generic one.
    public static string? CanonicalMimeForThreeD(string extension) => extension.ToLowerInvariant() switch
    {
        ".stl"  => "model/stl",
        ".obj"  => "model/obj",
        ".ply"  => "model/ply",
        ".glb"  => "model/gltf-binary",
        ".gltf" => "model/gltf+json",
        _       => null
    };
}
