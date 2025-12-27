using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace ClinicApp.Web.Models
{
    public class Material
{
    public int Id { get; set; }                 // Primary Key

    public string Name { get; set; } = null!;   // اسم المادة (Gloves, Mask…)

    public string? Description { get; set; }    // وصف اختياري

    public int Quantity { get; set; }           // الكمية الحالية في المخزن

    public int MinimumLimit { get; set; } 
     public string? Supplier { get; set; }       // أقل كمية قبل التحذير

    public string Unit { get; set; } = "pcs";   // وحدة القياس (pieces, box, ml…)

    public int ClinicId { get; set; }           // المادة تتبع لأي عيادة
    [ValidateNever]
    public Clinic Clinic { get; set; } = null!;
}

}