using System.Security.Claims;
using ClinicApp.Web.Data;
using ClinicApp.Web.Models;
using ClinicApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicApp.Web.Services;

namespace ClinicApp.Web.Controllers
{
    [Authorize(Roles = "Manager,Doctor")]
    public class MaterialsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IExportService _exportService;

        public MaterialsController(ApplicationDbContext context, IExportService exportService)
        {
            _context = context;
            _exportService = exportService;
        }

        private int GetCurrentClinicId()
        {
            var clinicClaim = User.FindFirst("ClinicId");
            if (clinicClaim == null)
                throw new InvalidOperationException("ClinicId claim is missing.");
            return int.Parse(clinicClaim.Value);
        }

        // GET: /Materials
        public async Task<IActionResult> Index()
        {
            var clinicId = GetCurrentClinicId();

            var materials = await _context.Materials
                .Where(m => m.ClinicId == clinicId)
                .OrderBy(m => m.Name)
                .ToListAsync();

            var vm = new MaterialsPageViewModel
            {
                Materials = materials,
                NewMaterial = new Material()
            };

            ViewData["Title"] = "Materials";
            return View(vm);
        }

        // POST: /Materials/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Add(MaterialsPageViewModel vm)
        {
            var clinicId = GetCurrentClinicId();

            if (!ModelState.IsValid)
            {
                vm.Materials = await _context.Materials
                    .Where(m => m.ClinicId == clinicId)
                    .OrderBy(m => m.Name)
                    .ToListAsync();
                return View("Index", vm);
            }

            var input = vm.NewMaterial;
            int addedQty = input.Quantity;

            var existing = await _context.Materials
                .FirstOrDefaultAsync(m => m.ClinicId == clinicId && m.Name == input.Name);

            Material material;
            if (existing == null)
            {
                material = new Material
                {
                    Name = input.Name,
                    Description = input.Description,
                    Supplier = input.Supplier,
                    Unit = input.Unit,
                    MinimumLimit = input.MinimumLimit,
                    Quantity = addedQty,
                    ClinicId = clinicId
                };
                _context.Materials.Add(material);
                await _context.SaveChangesAsync();
            }
            else
            {
                material = existing;
                existing.Quantity += addedQty;
                if (!string.IsNullOrWhiteSpace(input.Description)) existing.Description = input.Description;
                if (!string.IsNullOrWhiteSpace(input.Supplier))    existing.Supplier = input.Supplier;
            }

            var history = new MaterialHistory
            {
                MaterialId = material.Id,
                QuantityChange = addedQty,
                NewQuantity = material.Quantity,
                Supplier = input.Supplier,
                Note = existing == null ? "First stock added" : "Restock"
            };
            _context.MaterialHistories.Add(history);
            await _context.SaveChangesAsync();

            // Optional purchase invoice
            if (vm.Invoice.CreateInvoice && vm.Invoice.UnitPrice > 0 && addedQty > 0)
            {
                var invoice = new MaterialInvoice
                {
                    InvoiceNumber = await GenerateInvoiceNumberAsync(clinicId),
                    MaterialId = material.Id,
                    ClinicId = clinicId,
                    Supplier = input.Supplier,
                    Quantity = addedQty,
                    UnitPrice = vm.Invoice.UnitPrice,
                    TotalAmount = addedQty * vm.Invoice.UnitPrice,
                    PaymentMethod = vm.Invoice.PaymentMethod,
                    InvoiceDate = DateTime.UtcNow,
                    Notes = vm.Invoice.Notes,
                    CreatedAt = DateTime.UtcNow
                };
                _context.MaterialInvoices.Add(invoice);
                await _context.SaveChangesAsync();

                history.MaterialInvoiceId = invoice.Id;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Materials/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(
            int id, int change, string? note,
            bool createInvoice = false, decimal unitPrice = 0,
            PaymentMethod paymentMethod = PaymentMethod.Cash, string? invoiceNotes = null)
        {
            var clinicId = GetCurrentClinicId();

            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Id == id && m.ClinicId == clinicId);

            if (material == null) return NotFound();

            material.Quantity += change;
            if (material.Quantity < 0) material.Quantity = 0;

            var history = new MaterialHistory
            {
                MaterialId = material.Id,
                QuantityChange = change,
                NewQuantity = material.Quantity,
                Supplier = null,
                Note = note ?? (change > 0 ? "Quantity increased" : "Quantity decreased")
            };
            _context.MaterialHistories.Add(history);
            await _context.SaveChangesAsync();

            // Optional purchase invoice (only for positive changes)
            if (createInvoice && unitPrice > 0 && change > 0)
            {
                var invoice = new MaterialInvoice
                {
                    InvoiceNumber = await GenerateInvoiceNumberAsync(clinicId),
                    MaterialId = material.Id,
                    ClinicId = clinicId,
                    Supplier = material.Supplier,
                    Quantity = change,
                    UnitPrice = unitPrice,
                    TotalAmount = change * unitPrice,
                    PaymentMethod = paymentMethod,
                    InvoiceDate = DateTime.UtcNow,
                    Notes = invoiceNotes,
                    CreatedAt = DateTime.UtcNow
                };
                _context.MaterialInvoices.Add(invoice);
                await _context.SaveChangesAsync();

                history.MaterialInvoiceId = invoice.Id;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Materials/Invoices
        public async Task<IActionResult> Invoices(int? materialId)
        {
            var clinicId = GetCurrentClinicId();
            var now = DateTime.UtcNow;

            var query = _context.MaterialInvoices
                .Include(i => i.Material)
                .Where(i => i.ClinicId == clinicId);

            if (materialId.HasValue)
                query = query.Where(i => i.MaterialId == materialId.Value);

            var invoices = await query
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            var all = await _context.MaterialInvoices
                .Where(i => i.ClinicId == clinicId)
                .ToListAsync();

            var vm = new MaterialInvoicesViewModel
            {
                Invoices = invoices,
                FilterMaterialId = materialId,
                Materials = await _context.Materials
                    .Where(m => m.ClinicId == clinicId)
                    .OrderBy(m => m.Name)
                    .ToListAsync(),
                TotalThisMonth = all
                    .Where(i => i.InvoiceDate.Year == now.Year && i.InvoiceDate.Month == now.Month)
                    .Sum(i => i.TotalAmount),
                TotalThisYear = all
                    .Where(i => i.InvoiceDate.Year == now.Year)
                    .Sum(i => i.TotalAmount),
                TotalAllTime = all.Sum(i => i.TotalAmount)
            };

            ViewData["Title"] = "Material Purchase Invoices";
            return View(vm);
        }

        // GET: /Materials/History/{id}  →  JSON (includes invoice info)
        [HttpGet]
        public async Task<IActionResult> History(int id)
        {
            var clinicId = GetCurrentClinicId();

            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Id == id && m.ClinicId == clinicId);

            if (material == null) return NotFound();

            var history = await _context.MaterialHistories
                .Where(h => h.MaterialId == id)
                .Include(h => h.MaterialInvoice)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();

            var result = history.Select(h => new
            {
                h.Id,
                h.CreatedAt,
                h.QuantityChange,
                h.NewQuantity,
                h.Supplier,
                h.Note,
                InvoiceId = h.MaterialInvoiceId,
                InvoiceNumber = h.MaterialInvoice?.InvoiceNumber,
                InvoiceTotal = h.MaterialInvoice?.TotalAmount
            });

            return Json(result);
        }

        // POST: /Materials/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int id, string name, string? description, string unit, int minimumLimit, string? supplier)
        {
            var clinicId = GetCurrentClinicId();

            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Id == id && m.ClinicId == clinicId);

            if (material == null) return NotFound();

            material.Name = name;
            material.Description = description;
            material.Unit = unit;
            material.MinimumLimit = minimumLimit;
            material.Supplier = supplier;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: /Materials/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var clinicId = GetCurrentClinicId();

            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Id == id && m.ClinicId == clinicId);

            if (material == null) return NotFound();

            // Remove invoices first, then history, then material
            var invoiceIds = await _context.MaterialHistories
                .Where(h => h.MaterialId == id && h.MaterialInvoiceId != null)
                .Select(h => h.MaterialInvoiceId!.Value)
                .ToListAsync();

            var histories = _context.MaterialHistories.Where(h => h.MaterialId == id);
            _context.MaterialHistories.RemoveRange(histories);

            if (invoiceIds.Any())
            {
                var invoices = _context.MaterialInvoices.Where(i => invoiceIds.Contains(i.Id));
                _context.MaterialInvoices.RemoveRange(invoices);
            }

            _context.Materials.Remove(material);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Materials/ExportExcel
        [HttpGet]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ExportExcel()
        {
            var clinicId = GetCurrentClinicId();

            var materials = await _context.Materials
                .Where(m => m.ClinicId == clinicId)
                .OrderBy(m => m.Name)
                .ToListAsync();

            var materialIds = materials.Select(m => m.Id).ToList();
            var allHistory = await _context.MaterialHistories
                .Where(h => materialIds.Contains(h.MaterialId))
                .ToListAsync();

            var clinic = await _context.Clinics.FindAsync(clinicId);
            var clinicName = clinic?.Name ?? "Clinic";

            var fileBytes = _exportService.ExportInventoryToExcel(materials, clinicName, allHistory);

            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Inventory_Report_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        private async Task<string> GenerateInvoiceNumberAsync(int clinicId)
        {
            var prefix = $"MAT-{DateTime.UtcNow:yyyyMM}-";
            var count = await _context.MaterialInvoices
                .Where(i => i.ClinicId == clinicId && i.InvoiceNumber.StartsWith(prefix))
                .CountAsync();
            return $"{prefix}{(count + 1):D4}";
        }
    }
}
