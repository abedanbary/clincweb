using ClinicApp.Web.Data;
using ClinicApp.Web.Models;
using ClinicApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Web.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentClinicId()
        {
            var clinicClaim = User.FindFirst("ClinicId");
            if (clinicClaim == null)
                throw new InvalidOperationException("ClinicId claim is missing.");

            return int.Parse(clinicClaim.Value);
        }

        private string GetCurrentUserName()
        {
            var firstNameClaim = User.FindFirst("FirstName");
            var lastNameClaim = User.FindFirst("LastName");
            
            if (firstNameClaim != null && lastNameClaim != null)
                return $"{firstNameClaim.Value} {lastNameClaim.Value}";
            
            return "Doctor";
        }

        // GET: /Doctor
        public async Task<IActionResult> Index()
        {
            var clinicId = GetCurrentClinicId();
            var today = DateTime.UtcNow.Date;

            // Get clinic info
            var clinic = await _context.Clinics
                .FirstOrDefaultAsync(c => c.Id == clinicId);

            // Get today's appointments
            var todayAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.ClinicId == clinicId && a.StartTime.Date == today)
                .OrderBy(a => a.StartTime)
                .ToListAsync();

            // Get total patients count
            var totalPatients = await _context.Patients
                .CountAsync(p => p.ClinicId == clinicId);

            // Get pending appointments count
            var pendingAppointments = await _context.Appointments
                .CountAsync(a => a.ClinicId == clinicId && 
                                 a.StartTime.Date >= today && 
                                 a.Status == AppointmentStatus.Scheduled);

            // Get recent patients (last 5)
            var recentPatients = await _context.Patients
                .Where(p => p.ClinicId == clinicId)
                .OrderByDescending(p => p.Id)
                .Take(5)
                .ToListAsync();

            // Get low stock materials
            var lowStockMaterials = await _context.Materials
                .Where(m => m.ClinicId == clinicId && m.Quantity <= m.MinimumLimit)
                .OrderBy(m => m.Quantity)
                .Take(5)
                .ToListAsync();

            var vm = new DoctorViewModel
            {
                DoctorName = GetCurrentUserName(),
                ClinicName = clinic?.Name ?? "Clinic",
                TodayAppointmentsCount = todayAppointments.Count,
                TotalPatientsCount = totalPatients,
                PendingAppointmentsCount = pendingAppointments,
                LowStockMaterialsCount = lowStockMaterials.Count,
                TodayAppointments = todayAppointments,
                RecentPatients = recentPatients,
                LowStockMaterials = lowStockMaterials
            };

            ViewData["Title"] = "Dashboard";
            return View(vm);
        }
    }
}
