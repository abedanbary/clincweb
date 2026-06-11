using ClinicApp.Web.Models;

namespace ClinicApp.Web.ViewModels;

public class PatientFileViewerViewModel
{
    public int PatientId { get; set; }
    public int FileId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public PatientFileCategory Category { get; set; }
    public long Size { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public string? Notes { get; set; }
    public string PatientFirstName { get; set; } = string.Empty;
    public string PatientLastName { get; set; } = string.Empty;
}
