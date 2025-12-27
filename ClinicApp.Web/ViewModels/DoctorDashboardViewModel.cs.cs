using ClinicApp.Web.Models;

namespace ClinicApp.Web.ViewModels
{
    public class DoctorViewModel
    {
        // Statistics
        public int TodayAppointmentsCount { get; set; }
        public int TotalPatientsCount { get; set; }
        public int PendingAppointmentsCount { get; set; }
        public int LowStockMaterialsCount { get; set; }

        // Today's appointments
        public List<Appointment> TodayAppointments { get; set; } = new();

        // Recent patients
        public List<Patient> RecentPatients { get; set; } = new();

        // Low stock materials
        public List<Material> LowStockMaterials { get; set; } = new();

        // Doctor info
        public string DoctorName { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
    }
}