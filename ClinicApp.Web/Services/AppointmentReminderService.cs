using ClinicApp.Web.Data;
using ClinicApp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Web.Services
{
    public class AppointmentReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentReminderService> _logger;
        private readonly IAppTimeService _time;
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(60);

        public AppointmentReminderService(IServiceScopeFactory scopeFactory, ILogger<AppointmentReminderService> logger, IAppTimeService time)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _time = time;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "[Reminder] Service started. Check interval: {Interval} minutes.",
                CheckInterval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Reminder] Unexpected error during reminder check.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        internal async Task ProcessRemindersAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var whatsApp = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

            var utcNow = _time.UtcNow();
            var localNow = _time.LocalNow();
            var windowStart = utcNow.AddHours(23);
            var windowEnd = utcNow.AddHours(25);

            _logger.LogInformation("[Reminder] Check started | UTC: {Utc} | Israel: {Local} | Window: {Start} → {End}",
                utcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                localNow.ToString("yyyy-MM-dd HH:mm:ss"),
                windowStart.ToString("yyyy-MM-dd HH:mm:ss"),
                windowEnd.ToString("yyyy-MM-dd HH:mm:ss"));

            var appointments = await db.Appointments
                .Include(a => a.Patient)
                .Where(a =>
                    a.StartTime >= windowStart &&
                    a.StartTime <= windowEnd &&
                    a.Status != AppointmentStatus.Cancelled &&
                    !a.WhatsAppReminderSent)
                .ToListAsync();

            if (appointments.Count == 0)
            {
                _logger.LogInformation(
                    "[Reminder] No appointments found in the next 48 hours that need a reminder.");
                return;
            }

            _logger.LogInformation(
                "[Reminder] Found {Count} appointment(s) needing reminders.", appointments.Count);

            foreach (var appt in appointments)
            {
                var hasPhone = !string.IsNullOrWhiteSpace(appt.Patient?.Phone);
                _logger.LogInformation(
                    "[Reminder] Appointment {Id} | Patient {PatientId} | Start (UTC): {StartUtc} | Start (Local): {StartLocal} | " +
                    "Phone: {PhoneStatus} | ReminderSent: {Sent} | Status: {Status}",
                    appt.Id,
                    appt.PatientId,
                    appt.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    _time.ToLocal(appt.StartTime).ToString("yyyy-MM-dd HH:mm:ss"),
                    hasPhone ? "present" : "MISSING",
                    appt.WhatsAppReminderSent,
                    appt.Status);

                await SendReminderAsync(appt, whatsApp, db);
            }
        }

        private async Task SendReminderAsync(Appointment appointment, IWhatsAppService whatsApp, ApplicationDbContext db)
        {
            var (isValid, formatted, error) = PhoneNumberHelper.Format(appointment.Patient?.Phone);

            if (!isValid)
            {
                _logger.LogWarning(
                    "[Reminder] Skipping appointment {Id}: invalid phone — {Error}. Marking as sent to avoid repeat.",
                    appointment.Id, error);
                appointment.WhatsAppReminderSent = true;
                await db.SaveChangesAsync();
                return;
            }

            _logger.LogInformation(
                "[Reminder] Sending WhatsApp to {MaskedPhone} for appointment {Id}...",
                MaskPhone(formatted), appointment.Id);

            var (success, message) = await whatsApp.SendTemplateMessageAsync(formatted);

            if (success)
            {
                _logger.LogInformation(
                    "[Reminder] SUCCESS — appointment {Id} | phone {MaskedPhone} | response: {Message}",
                    appointment.Id, MaskPhone(formatted), message);
                appointment.WhatsAppReminderSent = true;
                await db.SaveChangesAsync();
            }
            else
            {
                _logger.LogError(
                    "[Reminder] FAILED — appointment {Id} | phone {MaskedPhone} | error: {Message}. Will retry next cycle.",
                    appointment.Id, MaskPhone(formatted), message);
            }
        }

        private static string MaskPhone(string phone) =>
            phone.Length > 7 ? phone[..4] + "****" + phone[^3..] : "****";
    }
}
