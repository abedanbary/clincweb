using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicApp.Web.Models
{
     public class Patient{
                  
    public int Id { get; set; }                      // Primary Key
    public string? IdNumber { get; set; }
    public string FirstName { get; set; } = null!;   // الاسم الأول
    public string LastName { get; set; } = null!;    // اسم العائلة

    public string Phone { get; set; } = null!;       // رقم الهاتف
    public string? Email { get; set; }               // الإيميل (اختياري)

    public DateTime DateOfBirth { get; set; }        // تاريخ الميلاد
    public string Gender { get; set; } = null!;      // Male / Female

    public string? Address { get; set; }             // العنوان

    public string? MedicalNotes { get; set; }        // ملاحظات طبية
    public string? Allergies { get; set; }           // حساسية دوائية
    public string? ChronicDiseases { get; set; }     // أمراض مزمنة

    public int ClinicId { get; set; }                // المريض يتبع لعيادة واحدة
     [ValidateNever]
    public Clinic Clinic { get; set; } = null!;
    [ValidateNever]
     public ICollection<PatientTooth> Teeth { get; set; } = new List<PatientTooth>();
     [ValidateNever]
     public ICollection<TreatmentPlan> TreatmentPlans { get; set; } = new List<TreatmentPlan>();
     public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
     [ValidateNever]
     public ICollection<PatientFile> PatientFiles { get; set; } = new List<PatientFile>();
    }
}
    