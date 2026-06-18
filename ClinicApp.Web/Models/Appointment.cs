using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicApp.Web.Models
{
    public class Appointment
   {
    public int Id { get; set; }                      // PK

    public DateTime StartTime { get; set; }          // وقت بداية الموعد
    public DateTime EndTime { get; set; }            // وقت نهاية الموعد

    public AppointmentStatus Status { get; set; } // حالة الموعد

    public string? ReasonForVisit { get; set; }  // سبب الزيارة
    public string? DoctorNotes { get; set; }     // ملاحظات الدكتور
    public string? Notes { get; set; }  
    
     public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
     public int? CreatedByUserId { get; set; }
    public AppUser? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }      
    public AppUser? UpdatedBy { get; set; }            


    public int PatientId { get; set; }
    [ValidateNever] 
    public Patient Patient { get; set; } = null!;

   // علاقة مع الطبيب
    public int DoctorId { get; set; }
    [ValidateNever] 
     public AppUser Doctor { get; set; } = null!;

    // العيادة
    public int ClinicId { get; set; }
    [ValidateNever]
    public Clinic Clinic { get; set; } = null!;

    public bool WhatsAppReminderSent { get; set; } = false;
 }

}
