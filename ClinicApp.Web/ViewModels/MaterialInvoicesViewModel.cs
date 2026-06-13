using ClinicApp.Web.Models;

namespace ClinicApp.Web.ViewModels;

public class MaterialInvoicesViewModel
{
    public List<MaterialInvoice> Invoices { get; set; } = new();
    public List<Material> Materials { get; set; } = new();
    public int? FilterMaterialId { get; set; }
    public decimal TotalThisMonth { get; set; }
    public decimal TotalThisYear { get; set; }
    public decimal TotalAllTime { get; set; }
}
