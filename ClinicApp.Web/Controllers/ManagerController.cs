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
            var now        = DateTime.UtcNow;
            var today      = now.Date;
            var yesterday  = today.AddDays(-1);
            var weekStart  = today.AddDays(-((int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1));
            var thisMonth  = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastMonth  = thisMonth.AddMonths(-1);
            var lastMonthEnd = thisMonth; // exclusive upper bound for last month

            // ── Clinic / Users ────────────────────────────────────────────────
            var clinic = await _context.Clinics.FirstOrDefaultAsync(c => c.Id == clinicId);

            var doctors = await _context.AppUsers
                .Where(u => u.ClinicId == clinicId && u.Role == UserRole.Doctor)
                .OrderBy(u => u.FirstName)
                .ToListAsync();

            // ── Appointments ──────────────────────────────────────────────────
            var todayAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.ClinicId == clinicId && a.StartTime.Date == today)
                .OrderBy(a => a.StartTime)
                .Take(8)
                .ToListAsync();

            var thisMonthAppts = await _context.Appointments
                .CountAsync(a => a.ClinicId == clinicId && a.StartTime >= thisMonth && a.StartTime < lastMonthEnd.AddMonths(1));

            var lastMonthAppts = await _context.Appointments
                .CountAsync(a => a.ClinicId == clinicId && a.StartTime >= lastMonth && a.StartTime < lastMonthEnd);

            var yesterdayAppts = await _context.Appointments
                .CountAsync(a => a.ClinicId == clinicId && a.StartTime.Date == yesterday);

            var thisWeekAppts = await _context.Appointments
                .CountAsync(a => a.ClinicId == clinicId && a.StartTime.Date >= weekStart && a.StartTime.Date <= today);

            // Today breakdown
            var todayAllStatuses = await _context.Appointments
                .Where(a => a.ClinicId == clinicId && a.StartTime.Date == today)
                .Select(a => a.Status)
                .ToListAsync();

            // ── Patients ──────────────────────────────────────────────────────
            var totalPatients = await _context.Patients.CountAsync(p => p.ClinicId == clinicId);

            // Use patient Id as a proxy for registration order (no CreatedAt on Patient model)
            var allPatientIds = await _context.Patients
                .Where(p => p.ClinicId == clinicId)
                .Select(p => p.Id)
                .ToListAsync();

            // Approximate "new this month" via appointments created this month for new patients
            // Since Patient has no CreatedAt, we use first-appointment proxy via appointment StartTime
            var patientFirstAppt = await _context.Appointments
                .Where(a => a.ClinicId == clinicId)
                .GroupBy(a => a.PatientId)
                .Select(g => new { PatientId = g.Key, FirstAppt = g.Min(a => a.StartTime) })
                .ToListAsync();

            var newPatientsThisMonth = patientFirstAppt.Count(p => p.FirstAppt >= thisMonth);
            var newPatientsLastMonth = patientFirstAppt.Count(p => p.FirstAppt >= lastMonth && p.FirstAppt < lastMonthEnd);

            var recentPatients = await _context.Patients
                .Where(p => p.ClinicId == clinicId)
                .OrderByDescending(p => p.Id)
                .Take(5)
                .ToListAsync();

            // ── Revenue / Payments ────────────────────────────────────────────
            var payments = await _context.Payments
                .Where(p => p.ClinicId == clinicId)
                .Select(p => new { p.Amount, p.Status, p.PaymentDate })
                .ToListAsync();

            var revenueThisMonth = payments
                .Where(p => p.Status == PaymentStatus.Paid && p.PaymentDate >= thisMonth)
                .Sum(p => p.Amount);

            var revenueLastMonth = payments
                .Where(p => p.Status == PaymentStatus.Paid && p.PaymentDate >= lastMonth && p.PaymentDate < lastMonthEnd)
                .Sum(p => p.Amount);

            var pendingTotal = payments
                .Where(p => p.Status == PaymentStatus.Pending)
                .Sum(p => p.Amount);

            // ── Treatments ────────────────────────────────────────────────────
            var treatments = await _context.Treatments
                .Where(t => t.ClinicId == clinicId && t.Status == TreatmentStatus.Completed)
                .Select(t => new { t.DoctorId, t.CompletedAt })
                .ToListAsync();

            var treatmentsThisMonth = treatments.Count(t => t.CompletedAt.HasValue && t.CompletedAt.Value >= thisMonth);
            var treatmentsLastMonth = treatments.Count(t => t.CompletedAt.HasValue && t.CompletedAt.Value >= lastMonth && t.CompletedAt.Value < lastMonthEnd);

            // ── Low Stock ─────────────────────────────────────────────────────
            var lowStockMaterials = await _context.Materials
                .Where(m => m.ClinicId == clinicId && m.Quantity <= m.MinimumLimit)
                .OrderBy(m => m.Quantity)
                .Take(5)
                .ToListAsync();

            // ── Last 7 days sparkline ─────────────────────────────────────────
            var allApptDates = await _context.Appointments
                .Where(a => a.ClinicId == clinicId && a.StartTime.Date >= today.AddDays(-6))
                .Select(a => a.StartTime.Date)
                .ToListAsync();

            var last7 = new List<int>();
            for (int offset = 6; offset >= 0; offset--)
            {
                var day = today.AddDays(-offset);
                last7.Add(allApptDates.Count(d => d == day));
            }

            // ── Top Doctors this month ────────────────────────────────────────
            var monthApptsByDoctor = await _context.Appointments
                .Where(a => a.ClinicId == clinicId && a.StartTime >= thisMonth)
                .GroupBy(a => a.DoctorId)
                .Select(g => new { DoctorId = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .ToListAsync();

            var doctorIds = monthApptsByDoctor.Select(x => x.DoctorId).ToList();

            var doctorUsers = await _context.AppUsers
                .Where(u => doctorIds.Contains(u.Id))
                .ToListAsync();

            // Treatments per doctor this month
            var treatsByDoctor = treatments
                .Where(t => t.CompletedAt.HasValue && t.CompletedAt.Value >= thisMonth && doctorIds.Contains(t.DoctorId))
                .GroupBy(t => t.DoctorId)
                .ToDictionary(g => g.Key, g => g.Count());

            // Attendance hours per doctor this month
            var attendances = await _context.DoctorAttendances
                .Where(a => a.ClinicId == clinicId && a.Status == AttendanceStatus.Present
                         && a.Date >= thisMonth && doctorIds.Contains(a.DoctorId)
                         && a.CheckIn != null && a.CheckOut != null)
                .Select(a => new { a.DoctorId, a.CheckIn, a.CheckOut })
                .ToListAsync();

            var hoursByDoctor = attendances
                .GroupBy(a => a.DoctorId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(a =>
                        (a.CheckOut.HasValue && a.CheckIn.HasValue && a.CheckOut.Value > a.CheckIn.Value)
                            ? (a.CheckOut.Value - a.CheckIn.Value).TotalHours
                            : 0));

            var topDoctors = monthApptsByDoctor.Select(x =>
            {
                var user = doctorUsers.FirstOrDefault(u => u.Id == x.DoctorId);
                return new DoctorMonthStats
                {
                    Name = user != null ? $"Dr. {user.FirstName} {user.LastName}" : "Unknown",
                    AppointmentsThisMonth = x.Count,
                    TreatmentsThisMonth = treatsByDoctor.GetValueOrDefault(x.DoctorId, 0),
                    HoursThisMonth = Math.Round(hoursByDoctor.GetValueOrDefault(x.DoctorId, 0), 1)
                };
            }).ToList();

            // ── Build ViewModel ───────────────────────────────────────────────
            var firstName = User.FindFirstValue("FirstName") ?? "";
            var lastName  = User.FindFirstValue("LastName") ?? "";

            var vm = new ManagerDashboardViewModel
            {
                ClinicName  = clinic?.Name ?? "Clinic",
                ManagerName = $"{firstName} {lastName}".Trim(),

                TotalDoctors  = doctors.Count,
                TotalPatients = totalPatients,
                LowStockMaterialsCount = lowStockMaterials.Count,

                TodayAppointmentsCount = todayAllStatuses.Count,
                YesterdayAppointments  = yesterdayAppts,
                ThisWeekAppointments   = thisWeekAppts,
                ThisMonthAppointments  = thisMonthAppts,
                LastMonthAppointments  = lastMonthAppts,

                TodayScheduled  = todayAllStatuses.Count(s => s == AppointmentStatus.Scheduled),
                TodayCompleted  = todayAllStatuses.Count(s => s == AppointmentStatus.Completed),
                TodayCancelled  = todayAllStatuses.Count(s => s == AppointmentStatus.Cancelled),
                TodayNoShow     = todayAllStatuses.Count(s => s == AppointmentStatus.NoShow),

                NewPatientsThisMonth = newPatientsThisMonth,
                NewPatientsLastMonth = newPatientsLastMonth,

                RevenueThisMonth    = revenueThisMonth,
                RevenueLastMonth    = revenueLastMonth,
                PendingPaymentsTotal = pendingTotal,

                TreatmentsThisMonth = treatmentsThisMonth,
                TreatmentsLastMonth = treatmentsLastMonth,

                Last7DayCounts = last7,
                TopDoctors     = topDoctors,

                TodayAppointments = todayAppointments,
                RecentPatients    = recentPatients,
                LowStockMaterials = lowStockMaterials,
                Doctors           = doctors
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
