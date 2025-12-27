using ClinicApp.Web.Models;

namespace ClinicApp.Web.ViewModels
{
    public class TreatmentPlanDetailsViewModel
    {
        public TreatmentPlan Plan { get; set; } = null!;
        
        public int TotalTreatments { get; set; }
        public int CompletedTreatments { get; set; }
        public int ProgressPercentage { get; set; }
        public decimal TotalActualCost { get; set; }
    }
}
