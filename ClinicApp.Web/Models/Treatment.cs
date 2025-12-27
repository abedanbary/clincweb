using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicApp.Web.Models
{
    public enum TreatmentStatus
    {
        Planned = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4
    }

    public enum TreatmentType
    {
        Cleaning = 1,
        Filling = 2,
        RootCanal = 3,
        Extraction = 4,
        Crown = 5,
        Bridge = 6,
        Implant = 7,
        Whitening = 8,
        Orthodontics = 9,
        Denture = 10,
        Scaling = 11,
        Other = 12
    }

    public class Treatment
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;
        public string? Description { get; set; }

        public decimal Cost { get; set; }                  // التكلفة الفعلية
        public decimal? EstimatedCost { get; set; }        // التكلفة المتوقعة

        public DateTime TreatmentDate { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // تفاصيل العلاج
        public int? ToothNumber { get; set; }              // رقم السن
        public TreatmentType Type { get; set; }
        public TreatmentStatus Status { get; set; } = TreatmentStatus.Planned;
        public int Priority { get; set; } = 2;

        // صور
        public string? BeforeImageUrl { get; set; }
        public string? AfterImageUrl { get; set; }

        // العلاقات
        public int PatientId { get; set; }
        [ValidateNever]
        public Patient Patient { get; set; } = null!;

        public int DoctorId { get; set; }
        [ValidateNever]
        public AppUser Doctor { get; set; } = null!;

        public int ClinicId { get; set; }
        [ValidateNever]
        public Clinic Clinic { get; set; } = null!;

        // 👇 ارتباط بخطة العلاج (اختياري)
        public int? TreatmentPlanId { get; set; }
        [ValidateNever]
        public TreatmentPlan? TreatmentPlan { get; set; }
    }
}