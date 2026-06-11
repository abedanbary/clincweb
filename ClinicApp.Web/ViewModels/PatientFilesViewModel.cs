using ClinicApp.Web.Models;

namespace ClinicApp.Web.ViewModels;

public class PatientFilesViewModel
{
    public int PatientId { get; set; }
    public string PatientFirstName { get; set; } = string.Empty;
    public string PatientLastName { get; set; } = string.Empty;
    public List<PatientFile> Files { get; set; } = new();
    public string? UploadError { get; set; }
}
