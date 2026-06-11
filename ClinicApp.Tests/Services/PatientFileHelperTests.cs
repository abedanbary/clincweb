using ClinicApp.Web.Models;
using ClinicApp.Web.Services.Storage;

namespace ClinicApp.Tests.Services;

public class PatientFileHelperTests
{
    // ── IsImage ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(".jpg",  true)]
    [InlineData(".jpeg", true)]
    [InlineData(".png",  true)]
    [InlineData(".webp", true)]
    [InlineData(".pdf",  false)]
    [InlineData(".stl",  false)]
    [InlineData(".glb",  false)]
    [InlineData(".exe",  false)]
    [InlineData("",      false)]
    public void IsImage_ReturnsExpected(string extension, bool expected) =>
        Assert.Equal(expected, PatientFileHelper.IsImage(extension));

    // ── IsPdf ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(".pdf",  true)]
    [InlineData(".PDF",  true)]  // case-insensitive
    [InlineData(".jpg",  false)]
    [InlineData(".stl",  false)]
    [InlineData("",      false)]
    public void IsPdf_ReturnsExpected(string extension, bool expected) =>
        Assert.Equal(expected, PatientFileHelper.IsPdf(extension));

    // ── IsThreeDModel ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(".stl",  true)]
    [InlineData(".obj",  true)]
    [InlineData(".ply",  true)]
    [InlineData(".glb",  true)]
    [InlineData(".gltf", true)]
    [InlineData(".STL",  true)]  // case-insensitive
    [InlineData(".jpg",  false)]
    [InlineData(".pdf",  false)]
    [InlineData(".exe",  false)]
    [InlineData("",      false)]
    public void IsThreeDModel_ReturnsExpected(string extension, bool expected) =>
        Assert.Equal(expected, PatientFileHelper.IsThreeDModel(extension));

    // ── GetCategorySlug ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(PatientFileCategory.IntraoralPhoto,  "intraoral-photo")]
    [InlineData(PatientFileCategory.PanoramicXray,   "panoramic-xray")]
    [InlineData(PatientFileCategory.BitewingXray,    "bitewing-xray")]
    [InlineData(PatientFileCategory.PeriapicalXray,  "periapical-xray")]
    [InlineData(PatientFileCategory.CbctScan,        "cbct-scan")]
    [InlineData(PatientFileCategory.IntraoralScan,   "intraoral-scan")]
    [InlineData(PatientFileCategory.MedicalReport,   "medical-report")]
    [InlineData(PatientFileCategory.Prescription,    "prescription")]
    [InlineData(PatientFileCategory.Invoice,         "invoice")]
    [InlineData(PatientFileCategory.Other,           "other")]
    public void GetCategorySlug_AllCategories_ReturnExpectedSlug(PatientFileCategory cat, string expected) =>
        Assert.Equal(expected, PatientFileHelper.GetCategorySlug(cat));

    [Fact]
    public void GetCategorySlug_UnknownValue_ReturnsOther() =>
        Assert.Equal("other", PatientFileHelper.GetCategorySlug((PatientFileCategory)99));

    [Fact]
    public void GetCategorySlug_NeverContainsSpaces()
    {
        foreach (PatientFileCategory cat in Enum.GetValues<PatientFileCategory>())
            Assert.DoesNotContain(" ", PatientFileHelper.GetCategorySlug(cat));
    }

    // ── GetDisplayName ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(PatientFileCategory.IntraoralPhoto, "Intraoral Photo")]
    [InlineData(PatientFileCategory.PanoramicXray,  "Panoramic X-ray")]
    [InlineData(PatientFileCategory.CbctScan,       "CBCT Scan")]
    [InlineData(PatientFileCategory.IntraoralScan,  "Intraoral Scan")]
    [InlineData(PatientFileCategory.MedicalReport,  "Medical Report")]
    [InlineData(PatientFileCategory.Other,          "Other")]
    public void GetDisplayName_ReturnsHumanReadable(PatientFileCategory cat, string expected) =>
        Assert.Equal(expected, PatientFileHelper.GetDisplayName(cat));

    [Fact]
    public void GetDisplayName_AllCategories_NeverNullOrEmpty()
    {
        foreach (PatientFileCategory cat in Enum.GetValues<PatientFileCategory>())
            Assert.False(string.IsNullOrWhiteSpace(PatientFileHelper.GetDisplayName(cat)));
    }

    // ── GetHumanReadableFileSize ───────────────────────────────────────────────

    [Theory]
    [InlineData(0,                  "0 B")]
    [InlineData(512,                "512 B")]
    [InlineData(1023,               "1023 B")]
    [InlineData(1024,               "1.0 KB")]
    [InlineData(1536,               "1.5 KB")]
    [InlineData(1024 * 1024 - 1,    "1024.0 KB")]
    [InlineData(1024 * 1024,        "1.0 MB")]
    [InlineData(50L * 1024 * 1024,  "50.0 MB")]
    [InlineData(200L * 1024 * 1024, "200.0 MB")]
    public void GetHumanReadableFileSize_ReturnsExpected(long bytes, string expected) =>
        Assert.Equal(expected, PatientFileHelper.GetHumanReadableFileSize(bytes));

    // ── CanonicalMimeForThreeD ─────────────────────────────────────────────────

    [Theory]
    [InlineData(".stl",  "model/stl")]
    [InlineData(".obj",  "model/obj")]
    [InlineData(".ply",  "model/ply")]
    [InlineData(".glb",  "model/gltf-binary")]
    [InlineData(".gltf", "model/gltf+json")]
    public void CanonicalMimeForThreeD_KnownExtension_ReturnsMime(string ext, string expected) =>
        Assert.Equal(expected, PatientFileHelper.CanonicalMimeForThreeD(ext));

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".pdf")]
    [InlineData(".exe")]
    [InlineData("")]
    public void CanonicalMimeForThreeD_NonThreeDExtension_ReturnsNull(string ext) =>
        Assert.Null(PatientFileHelper.CanonicalMimeForThreeD(ext));
}
