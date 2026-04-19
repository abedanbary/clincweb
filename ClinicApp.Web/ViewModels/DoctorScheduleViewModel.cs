using ClinicApp.Web.Models;

namespace ClinicApp.Web.ViewModels
{
    public class DoctorSchedulePageViewModel
    {
        public List<DoctorWithScheduleViewModel> Doctors { get; set; } = new();
    }

    public class DoctorWithScheduleViewModel
    {
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = null!;
        public List<DayScheduleViewModel> Schedule { get; set; } = new();
    }

    public class DayScheduleViewModel
    {
        public int? Id { get; set; }
        public int DoctorId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public string DayName => DayOfWeek.ToString();
        public bool IsWorkingDay { get; set; }
        public string StartTime { get; set; } = "09:00";
        public string EndTime { get; set; } = "17:00";
    }
}
