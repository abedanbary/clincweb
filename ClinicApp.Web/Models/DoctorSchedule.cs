using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicApp.Web.Models
{
    public class DoctorSchedule
    {
        public int Id { get; set; }

        public int DoctorId { get; set; }
        [ValidateNever]
        public AppUser Doctor { get; set; } = null!;

        public DayOfWeek DayOfWeek { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public bool IsWorkingDay { get; set; } = true;

        public int ClinicId { get; set; }
        [ValidateNever]
        public Clinic Clinic { get; set; } = null!;
    }
}
