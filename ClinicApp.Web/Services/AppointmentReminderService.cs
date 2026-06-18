using ClinicApp.Web.Data;
using ClinicApp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Web.Services
{
    public class AppointmentReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentReminderService> _logger;
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(30);

        public AppointmentReminderService(IServiceScopeFactory scopeFactory, ILogger<AppointmentReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Appointment reminder service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in appointment reminder service.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task ProcessRemindersAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var whatsApp = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

            var now = DateTime.UtcNow;
            // 24-hour window: send reminders for appointments 23–25 hours away
            var windowStart = now;
            var windowEnd = now.AddHours(48);

            var appointments = await db.Appointments
                .Include(a => a.Patient)
                .Where(a =>
                    a.StartTime >= windowStart &&
                    a.StartTime <= windowEnd &&
                    a.Status != AppointmentStatus.Cancelled &&
                    !a.WhatsAppReminderSent)
                .ToListAsync();

            if (appointments.Count == 0)
                return;

            _logger.LogInformation("Sending WhatsApp reminders for {Count} upcoming appointment(s).", appointments.Count);

            foreach (var appointment in appointments)
            {
                await SendReminderAsync(appointment, whatsApp, db);
            }
        }

        private async Task SendReminderAsync(Appointment appointment, IWhatsAppService whatsApp, ApplicationDbContext db)
        {
            var (isValid, formatted, error) = PhoneNumberHelper.Format(appointment.Patient?.Phone);

            if (!isValid)
            {
                // Mark as sent to prevent repeated log spam for permanently invalid numbers
                _logger.LogWarning(
                    "Skipping reminder for appointment {AppointmentId}: invalid phone — {Error}",
                    appointment.Id, error);
                appointment.WhatsAppReminderSent = true;
                await db.SaveChangesAsync();
                return;
            }

            var (success, message) = await whatsApp.SendTemplateMessageAsync(formatted);

            if (success)
            {
                _logger.LogInformation(
                    "Reminder sent for appointment {AppointmentId} to {MaskedPhone}.",
                    appointment.Id, MaskPhone(formatted));
                appointment.WhatsAppReminderSent = true;
                await db.SaveChangesAsync();
            }
            else
            {
                // Don't mark as sent — will retry on next cycle
                _logger.LogError(
                    "Failed to send reminder for appointment {AppointmentId}: {Message}",
                    appointment.Id, message);
            }
        }

        // Mask middle digits so full numbers never appear in logs
        private static string MaskPhone(string phone) =>
            phone.Length > 7 ? phone[..4] + "****" + phone[^3..] : "****";
    }
}
