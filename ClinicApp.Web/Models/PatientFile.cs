using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicApp.Web.Models;

public class PatientFile
{
    public int Id { get; set; }
    public int PatientId { get; set; }

    [ValidateNever]
    public Patient Patient { get; set; } = null!;

    public PatientFileCategory Category { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ObjectPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;

    public long Size { get; set; }

    public DateTime UploadedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public string? Notes { get; set; }
}
