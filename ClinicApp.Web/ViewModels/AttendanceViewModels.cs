using System.ComponentModel.DataAnnotations;
using ClinicApp.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClinicApp.Web.ViewModels
{
    public class AttendanceCalendarDay
    {
        public DateTime Date { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsToday { get; set; }
        public bool IsFuture { get; set; }
        public DoctorAttendance? Record { get; set; }
        public int TreatmentsCompleted { get; set; }
    }

    public class AttendanceMonthlySummary
    {
        public int DaysPresent { get; set; }
        public int DaysAbsent { get; set; }
        public int DayOff { get; set; }
        public int DaysNotMarked { get; set; }
        public double TotalHours { get; set; }
        public int TotalTreatments { get; set; }
        public double AvgTreatmentsPerDay =>
            DaysPresent > 0 ? Math.Round((double)TotalTreatments / DaysPresent, 1) : 0;
    }

    public class AttendancePageViewModel
    {
        public List<SelectListItem> Doctors { get; set; } = new();
        public int? SelectedDoctorId { get; set; }
        public string SelectedDoctorName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public List<AttendanceCalendarDay> CalendarDays { get; set; } = new();
        public AttendanceMonthlySummary Summary { get; set; } = new();
        public List<DoctorAttendance> Records { get; set; } = new();

        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
        public DateTime PrevMonth => new DateTime(Year, Month, 1).AddMonths(-1);
        public DateTime NextMonth => new DateTime(Year, Month, 1).AddMonths(1);
    }

    public class SaveAttendanceViewModel
    {
        public int? Id { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

        public string? CheckIn  { get; set; }   // "HH:mm"
        public string? CheckOut { get; set; }   // "HH:mm"

        public string? Notes { get; set; }
    }
}
