using ClinicApp.Web.Models;

namespace ClinicApp.Web.ViewModels;

public class MaterialInvoiceInput
{
    public bool CreateInvoice { get; set; }
    public decimal UnitPrice { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? Notes { get; set; }
}

public class MaterialsPageViewModel
{
    public List<Material> Materials { get; set; } = new();
    public Material NewMaterial { get; set; } = new();
    public MaterialInvoiceInput Invoice { get; set; } = new();
}
