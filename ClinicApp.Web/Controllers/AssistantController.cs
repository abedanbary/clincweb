using ClinicApp.Web.Data;
using ClinicApp.Web.Models;
using ClinicApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicApp.Web.Controllers
{
    [Authorize(Roles = "Assistant")]
    public class AssistantController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AssistantController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var clinicId = int.Parse(User.FindFirstValue("ClinicId")!);
            var today = DateTime.UtcNow.Date;

            var clinic = await _context.Clinics.FirstOrDefaultAsync(c => c.Id == clinicId);

            var todayAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.ClinicId == clinicId && a.StartTime.Date == today)
                .OrderBy(a => a.StartTime)
                .ToListAsync();

            var totalPatients = await _context.Patients.CountAsync(p => p.ClinicId == clinicId);

            var doctors = await _context.AppUsers
                .Where(u => u.ClinicId == clinicId && u.Role == UserRole.Doctor)
                .OrderBy(u => u.FirstName)
                .ToListAsync();

            var recentPatients = await _context.Patients
                .Where(p => p.ClinicId == clinicId)
                .OrderByDescending(p => p.Id)
                .Take(5)
                .ToListAsync();

            var lowStockMaterials = await _context.Materials
                .Where(m => m.ClinicId == clinicId && m.Quantity <= m.MinimumLimit)
                .OrderBy(m => m.Quantity)
                .Take(5)
                .ToListAsync();

            var firstName = User.FindFirstValue("FirstName") ?? "";
            var lastName  = User.FindFirstValue("LastName")  ?? "";

            var vm = new ManagerDashboardViewModel
            {
                ClinicName               = clinic?.Name ?? "Clinic",
                ManagerName              = $"{firstName} {lastName}".Trim(),
                TotalDoctors             = doctors.Count,
                TotalPatients            = totalPatients,
                TodayAppointmentsCount   = todayAppointments.Count,
                LowStockMaterialsCount   = lowStockMaterials.Count,
                TodayAppointments        = todayAppointments,
                RecentPatients           = recentPatients,
                LowStockMaterials        = lowStockMaterials,
                Doctors                  = doctors
            };

            return View(vm);
        }
    }
}
