using ClinicApp.Web.Models;

namespace ClinicApp.Web.ViewModels
{
    public class ManagerDashboardViewModel
    {
        public string ClinicName { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;

        // Stats
        public int TotalDoctors { get; set; }
        public int TotalPatients { get; set; }
        public int TodayAppointmentsCount { get; set; }
        public int LowStockMaterialsCount { get; set; }

        // Lists
        public List<Appointment> TodayAppointments { get; set; } = new();
        public List<Patient> RecentPatients { get; set; } = new();
        public List<Material> LowStockMaterials { get; set; } = new();
        public List<AppUser> Doctors { get; set; } = new();
    }
}
