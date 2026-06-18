using System.ComponentModel.DataAnnotations;
using ClinicApp.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClinicApp.Web.ViewModels
{
    public class PatientBalanceSummary
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public decimal TotalPaid { get; set; }
        public decimal TotalPending { get; set; }
        public int PendingCount { get; set; }
    }

    public class TreatmentPaymentSummary
    {
        public int TreatmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public TreatmentType Type { get; set; }
        public TreatmentStatus TreatmentStatus { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public decimal TotalCost { get; set; }
        public decimal TotalPaid { get; set; }
        public int PaidCount { get; set; }
        public int PendingCount { get; set; }
        public decimal Remaining => Math.Max(0, TotalCost - TotalPaid);
        public int ProgressPercent => TotalCost > 0 ? (int)Math.Min(100, TotalPaid / TotalCost * 100) : 0;
    }

    public class TreatmentPaymentsPageViewModel
    {
        public Treatment Treatment { get; set; } = null!;
        public Patient Patient { get; set; } = null!;
        public List<Payment> Payments { get; set; } = new();
        public decimal TotalCost { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalPending { get; set; }
        public decimal Remaining => Math.Max(0, TotalCost - TotalPaid);
        public int ProgressPercent => TotalCost > 0 ? (int)Math.Min(100, TotalPaid / TotalCost * 100) : 0;
        public AddInstallmentViewModel NewInstallment { get; set; } = new();
        public List<SelectListItem> Doctors { get; set; } = new();
    }

    public class AddInstallmentViewModel
    {
        [Required]
        public int TreatmentId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        [Range(0.01, 9_999_999, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        public PaymentMethod Method { get; set; }

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Paid;

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        public string? Notes { get; set; }
    }

    public class PaymentsPageViewModel
    {
        public List<Payment> Payments { get; set; } = new();

        // All-time stats
        public decimal TotalRevenue { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal RefundedAmount { get; set; }
        public int PaidCount { get; set; }
        public int PendingCount { get; set; }

        // This-month stats
        public decimal RevenueThisMonth { get; set; }
        public int PaidCountThisMonth { get; set; }

        // Per-patient pending lookup: PatientId → total pending amount
        public Dictionary<int, decimal> PatientPendingAmounts { get; set; } = new();

        // Patients with outstanding balances (pending > 0)
        public List<PatientBalanceSummary> OutstandingBalances { get; set; } = new();

        public int? FilterPatientId { get; set; }
        public string? FilterPatientName { get; set; }
        public PaymentStatus? FilterStatus { get; set; }
        public DateTime? FilterFrom { get; set; }
        public DateTime? FilterTo { get; set; }

        public List<SelectListItem> FilterPatients { get; set; } = new();
        public List<SelectListItem> Patients { get; set; } = new();
        public List<SelectListItem> Doctors { get; set; } = new();

        public CreatePaymentViewModel NewPayment { get; set; } = new();
    }

    public class CreatePaymentViewModel
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        public int? AppointmentId { get; set; }

        public int? TreatmentId { get; set; }

        [Required]
        [Range(0.01, 9_999_999, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        public PaymentMethod Method { get; set; }

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Paid;

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        public string? Notes { get; set; }
    }

    public class EditPaymentViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        public int? TreatmentId { get; set; }

        [Required]
        [Range(0.01, 9_999_999, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        public PaymentMethod Method { get; set; }

        [Required]
        public PaymentStatus Status { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        public string? Notes { get; set; }
    }
}
