using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicApp.Web.Models;

public class MaterialInvoice
{
    public int Id { get; set; }

    public string InvoiceNumber { get; set; } = null!;  // MAT-YYYYMM-NNNN

    public int MaterialId { get; set; }
    [ValidateNever]
    public Material Material { get; set; } = null!;

    public int ClinicId { get; set; }
    [ValidateNever]
    public Clinic Clinic { get; set; } = null!;

    public string? Supplier { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalAmount { get; set; }  // Quantity × UnitPrice

    public PaymentMethod PaymentMethod { get; set; }

    public DateTime InvoiceDate { get; set; }

    public string? Notes { get; set; }

    public string? InvoiceImageObjectPath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
