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

        // 🟦 GET: /Materials
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

        // 🟩 POST: /Materials/Add
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

            if (existing == null)
            {
                var newMaterial = new Material
                {
                    Name = input.Name,
                    Description = input.Description,
                    Supplier = input.Supplier,
                    Unit = input.Unit,
                    MinimumLimit = input.MinimumLimit,
                    Quantity = addedQty,
                    ClinicId = clinicId
                };

                _context.Materials.Add(newMaterial);
                await _context.SaveChangesAsync();

                _context.MaterialHistories.Add(new MaterialHistory
                {
                    MaterialId = newMaterial.Id,
                    QuantityChange = addedQty,
                    NewQuantity = newMaterial.Quantity,
                    Supplier = input.Supplier,
                    Note = "First stock added"
                });

                await _context.SaveChangesAsync();
            }
            else
            {
                existing.Quantity += addedQty;

                if (!string.IsNullOrWhiteSpace(input.Description))
                    existing.Description = input.Description;

                if (!string.IsNullOrWhiteSpace(input.Supplier))
                    existing.Supplier = input.Supplier;

                _context.MaterialHistories.Add(new MaterialHistory
                {
                    MaterialId = existing.Id,
                    QuantityChange = addedQty,
                    NewQuantity = existing.Quantity,
                    Supplier = input.Supplier,
                    Note = "Restock"
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // 🔵 GET: /Materials/History/{id}
        [HttpGet]
        public async Task<IActionResult> History(int id)
        {
            var clinicId = GetCurrentClinicId();

            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Id == id && m.ClinicId == clinicId);

            if (material == null)
                return NotFound();

            var history = await _context.MaterialHistories
                .Where(h => h.MaterialId == id)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();

            return Json(history);
        }

        // 🟡 POST: /Materials/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int id, string name, string? description, string unit, int minimumLimit, string? supplier)
        {
            var clinicId = GetCurrentClinicId();

            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Id == id && m.ClinicId == clinicId);

            if (material == null)
                return NotFound();

            material.Name = name;
            material.Description = description;
            material.Unit = unit;
            material.MinimumLimit = minimumLimit;
            material.Supplier = supplier;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // 🔴 POST: /Materials/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var clinicId = GetCurrentClinicId();

            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Id == id && m.ClinicId == clinicId);

            if (material == null)
                return NotFound();

            // حذف التاريخ أولاً
            var histories = _context.MaterialHistories.Where(h => h.MaterialId == id);
            _context.MaterialHistories.RemoveRange(histories);

            // ثم حذف المادة
            _context.Materials.Remove(material);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }



        // 🟢 POST: /Materials/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int id, int change, string? note)
        {
             var clinicId = GetCurrentClinicId();

             var material = await _context.Materials
             .FirstOrDefaultAsync(m => m.Id == id && m.ClinicId == clinicId);

             if (material == null)
               return NotFound();

             // Update quantity
             material.Quantity += change;

             // Prevent negative quantity
             if (material.Quantity < 0)
             material.Quantity = 0;

              // Save history
              _context.MaterialHistories.Add(new MaterialHistory
             {
                MaterialId = material.Id,
                QuantityChange = change,
                NewQuantity = material.Quantity,
                Supplier = null,
                Note = note ?? (change > 0 ? "Quantity increased" : "Quantity decreased")
                });

         await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
        }
        // 🟣 GET: /Materials/Export
        // 📊 Export to Excel
        [HttpGet]
        [Authorize(Roles = "Manager")]
         public async Task<IActionResult> ExportExcel()
         {  var clinicId = GetCurrentClinicId();
    
    var materials = await _context.Materials
        .Where(m => m.ClinicId == clinicId)
        .OrderBy(m => m.Name)
        .ToListAsync();
    
    var materialIds = materials.Select(m => m.Id).ToList();
    
    var allHistory = await _context.MaterialHistories
        .Where(h => materialIds.Contains(h.MaterialId))
        .ToListAsync();
    
    Console.WriteLine($"Materials count: {materials.Count}");
    Console.WriteLine($"History count: {allHistory.Count}");
    
    var clinic = await _context.Clinics.FindAsync(clinicId);
    var clinicName = clinic?.Name ?? "Clinic";
    
    var fileBytes = _exportService.ExportInventoryToExcel(materials, clinicName, allHistory);
    
    return File(fileBytes, 
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        $"Inventory_Report_{DateTime.Now:yyyyMMdd}.xlsx");
   
         }
    }
}