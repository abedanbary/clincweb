
namespace ClinicApp.Web.Models
{
    public class Payment
    {
        public int Id { get; set; }                                  // PK

        public int InvoiceNumber { get; set; }                       // رقم الفاتورة

        public decimal Amount { get; set; }                          // مبلغ الدفع

        public PaymentMethod Method { get; set; }                    // Enum → (Cash, Credit...)
        public PaymentStatus Status { get; set; }                    // Enum → (Paid, Pending...)

        public DateTime PaymentDate { get; set; }                    // تاريخ الدفع

        public string? Notes { get; set; }                           // ملاحظات

      
        public int PatientId { get; set; }                          
        public Patient Patient { get; set; } = null!;

       
        public int DoctorId { get; set; }
        public AppUser Doctor { get; set; } = null!;

      
        public int TreatmentId { get; set; }
        public Treatment Treatment { get; set; } = null!;

        public int ClinicId { get; set; }
        public Clinic Clinic { get; set; } = null!;
    }
}
