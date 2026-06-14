using ClinicApp.Web.Models;

namespace ClinicApp.Web.ViewModels
{
    public class DoctorMonthStats
    {
        public string Name { get; set; } = string.Empty;
        public int AppointmentsThisMonth { get; set; }
        public int TreatmentsThisMonth { get; set; }
        public double HoursThisMonth { get; set; }
    }

    public class ManagerDashboardViewModel
    {
        public string ClinicName { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;

        // ── Totals ─────────────────────────────────────────────────────────
        public int TotalDoctors { get; set; }
        public int TotalPatients { get; set; }
        public int LowStockMaterialsCount { get; set; }

        // ── Appointment counts ──────────────────────────────────────────────
        public int TodayAppointmentsCount { get; set; }
        public int YesterdayAppointments { get; set; }
        public int ThisWeekAppointments { get; set; }
        public int ThisMonthAppointments { get; set; }
        public int LastMonthAppointments { get; set; }

        // ── Today breakdown ─────────────────────────────────────────────────
        public int TodayScheduled { get; set; }
        public int TodayCompleted { get; set; }
        public int TodayCancelled { get; set; }
        public int TodayNoShow { get; set; }

        // ── Patients ────────────────────────────────────────────────────────
        public int NewPatientsThisMonth { get; set; }
        public int NewPatientsLastMonth { get; set; }

        // ── Revenue / Payments ──────────────────────────────────────────────
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueLastMonth { get; set; }
        public decimal PendingPaymentsTotal { get; set; }

        // ── Treatments ──────────────────────────────────────────────────────
        public int TreatmentsThisMonth { get; set; }
        public int TreatmentsLastMonth { get; set; }

        // ── Sparkline (last 7 days) ─────────────────────────────────────────
        /// <summary>Index 0 = 6 days ago, index 6 = today.</summary>
        public List<int> Last7DayCounts { get; set; } = new();

        // ── Doctor performance ───────────────────────────────────────────────
        public List<DoctorMonthStats> TopDoctors { get; set; } = new();

        // ── Lists ────────────────────────────────────────────────────────────
        public List<Appointment> TodayAppointments { get; set; } = new();
        public List<Patient> RecentPatients { get; set; } = new();
        public List<Material> LowStockMaterials { get; set; } = new();
        public List<AppUser> Doctors { get; set; } = new();
    }
}
