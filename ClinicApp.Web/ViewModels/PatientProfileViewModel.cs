using ClinicApp.Web.Models;

namespace ClinicApp.Web.ViewModels
{
    public class PatientProfileViewModel
    {
        // معلومات المريض
        public int PatientId { get; set; }
        public string? IdNumber { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? MedicalNotes { get; set; }
        public string? Allergies { get; set; }
        public string? ChronicDiseases { get; set; }

        // خريطة الأسنان (ToothNumber → {Status, Notes, UpdatedAt})
        public List<PatientTooth> Teeth { get; set; } = new();
    }
}