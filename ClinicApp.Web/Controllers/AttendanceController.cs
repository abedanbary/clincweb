using System.Security.Claims;
using ClinicApp.Web.Data;
using ClinicApp.Web.Models;
using ClinicApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Web.Controllers
{
    [Authorize(Roles = "Manager,Doctor,Assistant")]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /Attendance  — Manager view; Doctor gets redirected
        [HttpGet]
        public async Task<IActionResult> Index(int? doctorId, int? year, int? month)
        {
            if (User.IsInRole("Doctor"))
                return RedirectToAction(nameof(MyAttendance));

            var clinicId = GetCurrentClinicId();
            var now      = DateTime.UtcNow;
            var y        = year  ?? now.Year;
            var m        = month ?? now.Month;

            var doctors = await _context.AppUsers
                .Where(u => u.ClinicId == clinicId && u.Role == UserRole.Doctor)
                .OrderBy(u => u.FirstName)
                .ToListAsync();

            var selectedId   = doctorId ?? doctors.FirstOrDefault()?.Id;
            var selectedDoc  = doctors.FirstOrDefault(d => d.Id == selectedId);

            var vm = await BuildPageViewModel(clinicId, selectedId, y, m);
            vm.Doctors           = doctors.Select(d => new SelectListItem
            {
                Value    = d.Id.ToString(),
                Text     = $"Dr. {d.FirstName} {d.LastName}",
                Selected = d.Id == selectedId
            }).ToList();
            vm.SelectedDoctorName = selectedDoc != null
                ? $"Dr. {selectedDoc.FirstName} {selectedDoc.LastName}"
                : string.Empty;

            ViewData["Title"] = "Doctor Attendance";
            return View(vm);
        }

        // GET /Attendance/MyAttendance?year=&month=
        [HttpGet]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> MyAttendance(int? year, int? month)
        {
            var clinicId = GetCurrentClinicId();
            var doctorId = GetCurrentUserId();
            var now      = DateTime.UtcNow;
            var y        = year  ?? now.Year;
            var m        = month ?? now.Month;

            var vm = await BuildPageViewModel(clinicId, doctorId, y, m);

            var me = await _context.AppUsers.FindAsync(doctorId);
            vm.SelectedDoctorName = me != null ? $"Dr. {me.FirstName} {me.LastName}" : "Doctor";
            vm.SelectedDoctorId   = doctorId;

            ViewData["Title"] = "My Attendance";
            return View(vm);
        }

        // POST /Attendance/Save  — upsert one day's record
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(SaveAttendanceViewModel model)
        {
            var clinicId = GetCurrentClinicId();

            // Doctors can only save their own record
            if (User.IsInRole("Doctor") && model.DoctorId != GetCurrentUserId())
                return Forbid();

            var dateUtc = DateTime.SpecifyKind(model.Date.Date, DateTimeKind.Utc);

            TimeSpan? checkIn  = ParseTime(model.CheckIn);
            TimeSpan? checkOut = ParseTime(model.CheckOut);

            // Find existing record for that doctor + date
            var existing = await _context.DoctorAttendances
                .FirstOrDefaultAsync(a => a.DoctorId == model.DoctorId
                                       && a.Date     == dateUtc
                                       && a.ClinicId == clinicId);

            if (existing == null)
            {
                _context.DoctorAttendances.Add(new DoctorAttendance
                {
                    DoctorId  = model.DoctorId,
                    ClinicId  = clinicId,
                    Date      = dateUtc,
                    Status    = model.Status,
                    CheckIn   = model.Status == AttendanceStatus.Present ? checkIn  : null,
                    CheckOut  = model.Status == AttendanceStatus.Present ? checkOut : null,
                    Notes     = model.Notes,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Status    = model.Status;
                existing.CheckIn   = model.Status == AttendanceStatus.Present ? checkIn  : null;
                existing.CheckOut  = model.Status == AttendanceStatus.Present ? checkOut : null;
                existing.Notes     = model.Notes;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Attendance saved.";

            return User.IsInRole("Doctor")
                ? RedirectToAction(nameof(MyAttendance), new { year = model.Date.Year, month = model.Date.Month })
                : RedirectToAction(nameof(Index),        new { doctorId = model.DoctorId, year = model.Date.Year, month = model.Date.Month });
        }

        // POST /Attendance/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int id, int doctorId, int year, int month)
        {
            var clinicId = GetCurrentClinicId();
            var record   = await _context.DoctorAttendances
                .FirstOrDefaultAsync(a => a.Id == id && a.ClinicId == clinicId);

            if (record != null)
            {
                _context.DoctorAttendances.Remove(record);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Record deleted.";
            }

            return RedirectToAction(nameof(Index), new { doctorId, year, month });
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private async Task<AttendancePageViewModel> BuildPageViewModel(
            int clinicId, int? doctorId, int year, int month)
        {
            var firstDay = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastDay  = firstDay.AddMonths(1);
            var today    = DateTime.UtcNow.Date;

            var records = doctorId.HasValue
                ? await _context.DoctorAttendances
                    .Include(a => a.Doctor)
                    .Where(a => a.DoctorId == doctorId.Value
                             && a.ClinicId == clinicId
                             && a.Date     >= firstDay
                             && a.Date     <  lastDay)
                    .OrderBy(a => a.Date)
                    .ToListAsync()
                : new List<DoctorAttendance>();

            var treatments = doctorId.HasValue
                ? await _context.Treatments
                    .Where(t => t.DoctorId == doctorId.Value
                             && t.ClinicId == clinicId
                             && t.Status   == TreatmentStatus.Completed
                             && t.CompletedAt.HasValue
                             && t.CompletedAt.Value >= firstDay
                             && t.CompletedAt.Value <  lastDay)
                    .ToListAsync()
                : new List<Treatment>();

            // Build calendar grid starting from Monday of the week of the 1st
            var calStart = firstDay.AddDays(-(((int)firstDay.DayOfWeek + 6) % 7));
            var calDays  = new List<AttendanceCalendarDay>();
            var cursor   = calStart;
            while (cursor < lastDay || calDays.Count % 7 != 0)
            {
                calDays.Add(new AttendanceCalendarDay
                {
                    Date             = cursor,
                    IsCurrentMonth   = cursor.Month == month,
                    IsToday          = cursor.Date  == today,
                    IsFuture         = cursor.Date  > today,
                    Record           = records.FirstOrDefault(r => r.Date.Date == cursor.Date),
                    TreatmentsCompleted = treatments.Count(t =>
                        t.CompletedAt.HasValue && t.CompletedAt.Value.Date == cursor.Date)
                });
                cursor = cursor.AddDays(1);
            }

            // Summary
            var presentDays = records.Count(r => r.Status == AttendanceStatus.Present);
            var workDaysInMonth = Enumerable.Range(0, (lastDay - firstDay).Days)
                .Select(i => firstDay.AddDays(i))
                .Count(d => d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Friday
                         && d.Date <= today);

            var summary = new AttendanceMonthlySummary
            {
                DaysPresent      = presentDays,
                DaysAbsent       = records.Count(r => r.Status == AttendanceStatus.Absent),
                DayOff           = records.Count(r => r.Status == AttendanceStatus.DayOff),
                DaysNotMarked    = Math.Max(0, workDaysInMonth - records.Count),
                TotalHours       = Math.Round(records
                    .Where(r => r.WorkingHours.HasValue)
                    .Sum(r => r.WorkingHours!.Value), 1),
                TotalTreatments  = treatments.Count
            };

            return new AttendancePageViewModel
            {
                SelectedDoctorId = doctorId,
                Year             = year,
                Month            = month,
                CalendarDays     = calDays,
                Summary          = summary,
                Records          = records
            };
        }

        private static TimeSpan? ParseTime(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            return TimeSpan.TryParse(input, out var ts) ? ts : null;
        }

        private int GetCurrentClinicId() =>
            int.Parse(User.FindFirstValue("ClinicId")!);

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
