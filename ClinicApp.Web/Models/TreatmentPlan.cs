using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicApp.Web.Models
{
    public enum PlanStatus
    {
        Draft = 1,        // مسودة
        Active = 2,       // نشطة
        Completed = 3,    // مكتملة
        Cancelled = 4     // ملغية
    }

    public class TreatmentPlan
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;  // مثلاً: "خطة علاج شاملة - نوفمبر 2024"
        
        public string? Description { get; set; }    // وصف عام للخطة

        public decimal TotalEstimatedCost { get; set; }  // إجمالي التكلفة المتوقعة

        public PlanStatus Status { get; set; } = PlanStatus.Draft;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? StartDate { get; set; }    // تاريخ البدء
        
        public DateTime? CompletedAt { get; set; }  // تاريخ الانتهاء

        // المريض
        public int PatientId { get; set; }
        [ValidateNever]
        public Patient Patient { get; set; } = null!;

        // الطبيب المسؤول
        public int DoctorId { get; set; }
        [ValidateNever]
        public AppUser Doctor { get; set; } = null!;

        // العيادة
        public int ClinicId { get; set; }
        [ValidateNever]
        public Clinic Clinic { get; set; } = null!;

        // 👇 العلاقة - الخطة تحتوي على عدة إجراءات
        [ValidateNever]
        public ICollection<Treatment> Treatments { get; set; } = new List<Treatment>();
    }
}