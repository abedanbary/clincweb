using ClinicApp.Web.Models;
using System.ComponentModel.DataAnnotations;

namespace ClinicApp.Web.ViewModels
{
    public class EditAppointmentViewModel
    {
        public int Id { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public DateTime? StartTime { get; set; }

        [Required]
        public DateTime? EndTime { get; set; }

        public string? ReasonForVisit { get; set; }
        
        public string? Notes { get; set; }

        [Required]
        public AppointmentStatus Status { get; set; }
    }
}