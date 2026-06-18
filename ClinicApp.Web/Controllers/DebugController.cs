using ClinicApp.Web.Data;
using ClinicApp.Web.Models;
using ClinicApp.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Web.Controllers
{
    [Route("api/debug")]
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IWhatsAppService _whatsApp;
        private readonly AppointmentReminderService _reminderService;
        private readonly IWebHostEnvironment _env;

        private static readonly TimeZoneInfo IsraelTz =
            TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "Israel Standard Time" : "Asia/Jerusalem");

        public DebugController(
            ApplicationDbContext db,
            IWhatsAppService whatsApp,
            AppointmentReminderService reminderService,
            IWebHostEnvironment env)
        {
            _db = db;
            _whatsApp = whatsApp;
            _reminderService = reminderService;
            _env = env;
        }

        // GET /api/debug/whatsapp-reminders
        [HttpGet("whatsapp-reminders")]
        public async Task<IActionResult> GetDebugInfo()
        {
            if (!IsAllowed()) return Forbid();

            var utcNow = DateTime.UtcNow;
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, IsraelTz);
            var windowStart = utcNow;
            var windowEnd = utcNow.AddHours(48);

            var totalAppointments = await _db.Appointments.CountAsync();

            var upcoming = await _db.Appointments
                .Include(a => a.Patient)
                .Where(a => a.StartTime >= utcNow && a.StartTime <= windowEnd)
                .OrderBy(a => a.StartTime)
                .Take(20)
                .ToListAsync();

            var inWindow = upcoming
                .Where(a => a.Status != AppointmentStatus.Cancelled && !a.WhatsAppReminderSent)
                .ToList();

            return Ok(new
            {
                utcNow = utcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                localNow = localNow.ToString("yyyy-MM-dd HH:mm:ss") + " (Israel)",
                windowStart = windowStart.ToString("yyyy-MM-dd HH:mm:ss"),
                windowEnd = windowEnd.ToString("yyyy-MM-dd HH:mm:ss"),
                whatsAppConfig = new
                {
                    hasAccessToken = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WHATSAPP_ACCESS_TOKEN")),
                    hasPhoneNumberId = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WHATSAPP_PHONE_NUMBER_ID")),
                    isConfigured = _whatsApp.IsConfigured
                },
                totalAppointmentsInDatabase = totalAppointments,
                appointmentsInsideReminderWindow = inWindow.Count,
                upcomingAppointmentsNext48Hours = upcoming.Select(a => new
                {
                    id = a.Id,
                    patientId = a.PatientId,
                    startTime = a.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    status = a.Status.ToString(),
                    reminderSent = a.WhatsAppReminderSent,
                    hasPhone = !string.IsNullOrWhiteSpace(a.Patient?.Phone),
                    maskedPhone = MaskPhone(a.Patient?.Phone),
                    wouldSend = a.Status != AppointmentStatus.Cancelled && !a.WhatsAppReminderSent
                })
            });
        }

        // POST /api/debug/whatsapp-reminders/run
        [HttpPost("whatsapp-reminders/run")]
        public async Task<IActionResult> RunNow()
        {
            if (!IsAllowed()) return Forbid();

            var started = DateTime.UtcNow;
            await _reminderService.ProcessRemindersAsync();
            var elapsed = (DateTime.UtcNow - started).TotalSeconds;

            return Ok(new
            {
                triggered = true,
                ranAt = started.ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
                elapsedSeconds = Math.Round(elapsed, 2),
                note = "Check application logs for detailed results."
            });
        }

        private bool IsAllowed() =>
            _env.IsDevelopment() || User.IsInRole("Manager");

        private static string? MaskPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            return digits.Length > 7 ? digits[..4] + "****" + digits[^3..] : "****";
        }
    }
}
