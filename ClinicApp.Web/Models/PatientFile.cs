using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicApp.Web.Models;

public class PatientFile
{
    public int Id { get; set; }
    public int PatientId { get; set; }

    [ValidateNever]
    public Patient Patient { get; set; } = null!;

    public string OriginalFileName { get; set; } = null!;
    public string ObjectPath { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long Size { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public string? Description { get; set; }
}
