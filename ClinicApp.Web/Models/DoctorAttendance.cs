using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicApp.Web.Models
{
    public class DoctorAttendance
    {
        public int Id { get; set; }

        public int DoctorId { get; set; }
        [ValidateNever]
        public AppUser Doctor { get; set; } = null!;

        public int ClinicId { get; set; }
        [ValidateNever]
        public Clinic Clinic { get; set; } = null!;

        public DateTime Date { get; set; }          // UTC midnight — represents the calendar day

        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

        public TimeSpan? CheckIn  { get; set; }     // time of day, e.g. 09:00
        public TimeSpan? CheckOut { get; set; }     // time of day, e.g. 17:30

        public string? Notes { get; set; }

        public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Not mapped — computed on the fly
        public double? WorkingHours =>
            CheckIn.HasValue && CheckOut.HasValue && CheckOut.Value > CheckIn.Value
                ? Math.Round((CheckOut.Value - CheckIn.Value).TotalHours, 1)
                : null;
    }
}
