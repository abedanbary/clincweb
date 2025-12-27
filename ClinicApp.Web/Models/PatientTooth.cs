using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicApp.Web.Models
{
    public enum ToothStatus
    {
        Healthy = 1,      // سليم
        Caries = 2,       // تسوس
        Filled = 3,       // حشوة
        Crown = 4,        // تاج
        RootCanal = 5,    // علاج جذور
        Missing = 6,      // مفقود
        Extraction = 7,   // مطلوب خلع
        Other = 8         // أخرى
    }

    public class PatientTooth
    {
        public int Id { get; set; }

        // FK to Patient
        public int PatientId { get; set; }
        
        [ValidateNever]
        public Patient Patient { get; set; } = null!;

        // رقم السن (FDI: 11, 12, 21, 22, 36, 37... etc)
        public int ToothNumber { get; set; }

        // الحالة الحالية
        public ToothStatus Status { get; set; }

        // ملاحظات إضافية
        public string? Notes { get; set; }

        // تاريخ آخر تحديث

       public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
    }
}