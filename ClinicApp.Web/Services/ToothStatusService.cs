using ClinicApp.Web.Data;
using ClinicApp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Web.Services
{
    public class ToothStatusService : IToothStatusService
    {
        private readonly ApplicationDbContext _context;

        public ToothStatusService(ApplicationDbContext context)
        {
            _context = context;
        }

        public ToothStatus ComputeFromTreatments(IEnumerable<Treatment> treatments, ToothStatus fallback = ToothStatus.Healthy)
        {
            var active = treatments.Where(t => t.Status != TreatmentStatus.Cancelled).ToList();
            if (!active.Any()) return fallback;

            // Highest-priority state: any in-progress treatment
            if (active.Any(t => t.Status == TreatmentStatus.InProgress))
                return ToothStatus.InProgress;

            // Planned treatments — extraction takes precedence over generic planned
            var planned = active.Where(t => t.Status == TreatmentStatus.Planned).ToList();
            if (planned.Any())
            {
                return planned.Any(t => t.Type == TreatmentType.Extraction)
                    ? ToothStatus.Extraction
                    : ToothStatus.PlannedTreatment;
            }

            // All active treatments are completed — derive visual state from most recent structural treatment.
            // Non-structural treatments (cleaning, scaling, whitening, etc.) resolve to Healthy
            // because they don't permanently alter the tooth.
            var latest = active.OrderByDescending(t => t.CompletedAt ?? t.TreatmentDate).First();
            return latest.Type switch
            {
                TreatmentType.Extraction                          => ToothStatus.Missing,
                TreatmentType.Crown or TreatmentType.Bridge       => ToothStatus.Crown,
                TreatmentType.Implant                             => ToothStatus.Implant,
                TreatmentType.RootCanal                           => ToothStatus.RootCanal,
                TreatmentType.Filling                             => ToothStatus.Filled,
                _                                                 => ToothStatus.Healthy
            };
        }

        public async Task SyncToothAsync(int patientId, int toothNumber, int clinicId)
        {
            var treatments = await _context.Treatments
                .Where(t => t.PatientId == patientId && t.ToothNumber == toothNumber && t.ClinicId == clinicId)
                .ToListAsync();

            var tooth = await _context.PatientTeeth
                .FirstOrDefaultAsync(t => t.PatientId == patientId && t.ToothNumber == toothNumber);

            var hasActive = treatments.Any(t => t.Status != TreatmentStatus.Cancelled);
            var newStatus = ComputeFromTreatments(treatments, tooth?.Status ?? ToothStatus.Healthy);

            if (tooth == null)
            {
                if (hasActive)
                {
                    _context.PatientTeeth.Add(new PatientTooth
                    {
                        PatientId = patientId,
                        ToothNumber = toothNumber,
                        Status = newStatus,
                        UpdatedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                tooth.Status = newStatus;
                tooth.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
