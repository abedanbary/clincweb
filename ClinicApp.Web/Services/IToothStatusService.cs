using ClinicApp.Web.Models;

namespace ClinicApp.Web.Services
{
    public interface IToothStatusService
    {
        /// <summary>Derives ToothStatus from the treatments linked to a tooth.</summary>
        ToothStatus ComputeFromTreatments(IEnumerable<Treatment> treatments, ToothStatus fallback = ToothStatus.Healthy);

        /// <summary>Re-queries treatments for the given tooth and persists the computed ToothStatus to PatientTooth.</summary>
        Task SyncToothAsync(int patientId, int toothNumber, int clinicId);
    }
}
