using ClinicApp.Web.Data;
using ClinicApp.Web.Models;
using ClinicApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicApp.Web.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ManagerController(ApplicationDbContext context)
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
            var lastName = User.FindFirstValue("LastName") ?? "";

            var vm = new ManagerDashboardViewModel
            {
                ClinicName = clinic?.Name ?? "Clinic",
                ManagerName = $"{firstName} {lastName}".Trim(),
                TotalDoctors = doctors.Count,
                TotalPatients = totalPatients,
                TodayAppointmentsCount = todayAppointments.Count,
                LowStockMaterialsCount = lowStockMaterials.Count,
                TodayAppointments = todayAppointments,
                RecentPatients = recentPatients,
                LowStockMaterials = lowStockMaterials,
                Doctors = doctors
            };

            return View(vm);
        }

        public IActionResult Materials()
        {
            return View();
        }

        // GET: /Manager/DoctorSchedules
        public async Task<IActionResult> DoctorSchedules()
        {
            var clinicId = int.Parse(User.FindFirstValue("ClinicId")!);

            var doctors = await _context.AppUsers
                .Where(u => u.ClinicId == clinicId && u.Role == UserRole.Doctor)
                .OrderBy(u => u.FirstName)
                .ToListAsync();

            var existingSchedules = await _context.DoctorSchedules
                .Where(s => s.ClinicId == clinicId)
                .ToListAsync();

            var allDays = Enum.GetValues<DayOfWeek>().OrderBy(d => ((int)d + 6) % 7).ToList(); // Mon-Sun order

            var vm = new DoctorSchedulePageViewModel
            {
                Doctors = doctors.Select(doc => new DoctorWithScheduleViewModel
                {
                    DoctorId = doc.Id,
                    DoctorName = $"Dr. {doc.FirstName} {doc.LastName}",
                    Schedule = allDays.Select(day =>
                    {
                        var existing = existingSchedules
                            .FirstOrDefault(s => s.DoctorId == doc.Id && s.DayOfWeek == day);
                        return new DayScheduleViewModel
                        {
                            Id = existing?.Id,
                            DoctorId = doc.Id,
                            DayOfWeek = day,
                            IsWorkingDay = existing?.IsWorkingDay ?? false,
                            StartTime = existing != null ? existing.StartTime.ToString(@"hh\:mm") : "09:00",
                            EndTime = existing != null ? existing.EndTime.ToString(@"hh\:mm") : "17:00"
                        };
                    }).ToList()
                }).ToList()
            };

            return View(vm);
        }

        // POST: /Manager/SaveDoctorSchedules
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDoctorSchedules(List<DayScheduleViewModel> schedules)
        {
            var clinicId = int.Parse(User.FindFirstValue("ClinicId")!);

            foreach (var item in schedules)
            {
                if (item.Id.HasValue)
                {
                    var existing = await _context.DoctorSchedules.FindAsync(item.Id.Value);
                    if (existing != null)
                    {
                        existing.IsWorkingDay = item.IsWorkingDay;
                        existing.StartTime = TimeSpan.Parse(item.StartTime);
                        existing.EndTime = TimeSpan.Parse(item.EndTime);
                    }
                }
                else
                {
                    _context.DoctorSchedules.Add(new DoctorSchedule
                    {
                        DoctorId = item.DoctorId,
                        DayOfWeek = item.DayOfWeek,
                        IsWorkingDay = item.IsWorkingDay,
                        StartTime = TimeSpan.Parse(item.StartTime),
                        EndTime = TimeSpan.Parse(item.EndTime),
                        ClinicId = clinicId
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Doctor schedules saved successfully!";
            return RedirectToAction(nameof(DoctorSchedules));
        }
    }
}
