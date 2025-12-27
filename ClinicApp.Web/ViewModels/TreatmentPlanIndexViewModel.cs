using ClinicApp.Web.Models;

namespace ClinicApp.Web.ViewModels
{
    public class TreatmentPlanIndexViewModel
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public List<TreatmentPlan> Plans { get; set; } = new();
    }
}
