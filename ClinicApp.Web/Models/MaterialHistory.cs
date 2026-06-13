namespace ClinicApp.Web.Models{

public class MaterialHistory
{
    public int Id { get; set; }

    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;

    // الكمية التي تمت إضافتها
    public int QuantityChange { get; set; }

    // الكمية الجديدة بعد الإضافة
    public int NewQuantity { get; set; }

    public string? Supplier { get; set; }  // ⭐ مجرد نص

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? Note { get; set; }

    public int? MaterialInvoiceId { get; set; }
    public MaterialInvoice? MaterialInvoice { get; set; }
}
}