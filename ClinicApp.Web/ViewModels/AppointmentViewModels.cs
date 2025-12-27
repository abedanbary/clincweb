using System.ComponentModel.DataAnnotations;
using ClinicApp.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClinicApp.Web.ViewModels
{
    // ViewModel للإنشاء
    public class CreateAppointmentViewModel
    {
        public bool IsNewPatient { get; set; } = false;

        // مريض موجود
        public int? ExistingPatientId { get; set; }

        // مريض جديد
        public string? NewPatientFirstName { get; set; }
        public string? NewPatientLastName { get; set; }
        public string? NewPatientPhone { get; set; }
        public string? NewPatientEmail { get; set; }
        public DateTime? NewPatientDateOfBirth { get; set; }
        public string? NewPatientGender { get; set; }
        public string? NewPatientAddress { get; set; }
        public string? NewPatientMedicalHistory { get; set; }

        // بيانات الموعد
        [Required(ErrorMessage = "Please select a doctor")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Please select start time")]
        public DateTime? StartTime { get; set; }

        [Required(ErrorMessage = "Please select end time")]
        public DateTime? EndTime { get; set; }

        public string? ReasonForVisit { get; set; }
        public string? Notes { get; set; }

        // للعرض
        public List<SelectListItem>? ExistingPatients { get; set; }
        public List<SelectListItem>? Doctors { get; set; }
    }

    // ViewModel للتعديل
   
}