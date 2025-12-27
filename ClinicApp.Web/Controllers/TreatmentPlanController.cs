using ClinicApp.Web.Data;
using ClinicApp.Web.Models;
using ClinicApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicApp.Web.Services;

namespace ClinicApp.Web.Controllers
{
    [Authorize(Roles = "Doctor,Manager")]
    public class TreatmentPlanController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICloudinaryService _cloudinary;

        public TreatmentPlanController(ApplicationDbContext context,ICloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary=cloudinary;
        }

        private int GetCurrentClinicId()
        {
            var clinicClaim = User.FindFirst("ClinicId");
            if (clinicClaim == null)
                throw new InvalidOperationException("ClinicId claim is missing.");

            return int.Parse(clinicClaim.Value);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null)
                throw new InvalidOperationException("UserId claim is missing.");

            return int.Parse(userIdClaim.Value);
        }

        // 🟦 GET: /TreatmentPlan/Index/5 (Patient ID)
        public async Task<IActionResult> Index(int id)
        {
            var clinicId = GetCurrentClinicId();

            // تحقق من وجود المريض
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == id && p.ClinicId == clinicId);

            if (patient == null)
                return NotFound();

            // جلب جميع الخطط للمريض
            var plans = await _context.TreatmentPlans
                .Include(p => p.Treatments)
                .Include(p => p.Doctor)
                .Where(p => p.PatientId == id && p.ClinicId == clinicId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var vm = new TreatmentPlanIndexViewModel
            {
                PatientId = patient.Id,
                PatientName = $"{patient.FirstName} {patient.LastName}",
                Plans = plans
            };

            ViewData["Title"] = "Treatment Plans";
            return View(vm);
        }

        // 🟩 POST: /TreatmentPlan/CreatePlan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePlan(int patientId, string title, string? description, DateTime? startDate)
        {
            var clinicId = GetCurrentClinicId();
            var doctorId = GetCurrentUserId();

            // تحقق من المريض
            var patientExists = await _context.Patients
                .AnyAsync(p => p.Id == patientId && p.ClinicId == clinicId);

            if (!patientExists)
                return NotFound();

            var plan = new TreatmentPlan
            {
                Title = title,
                Description = description,
                StartDate = startDate.HasValue ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc) : null,
                Status = PlanStatus.Draft,
                TotalEstimatedCost = 0,
                PatientId = patientId,
                DoctorId = doctorId,
                ClinicId = clinicId,
                CreatedAt = DateTime.UtcNow
            };

            _context.TreatmentPlans.Add(plan);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = plan.Id });
        }

        // 🟦 GET: /TreatmentPlan/Details/5 (Plan ID)
        public async Task<IActionResult> Details(int id)
        {
            var clinicId = GetCurrentClinicId();

            var plan = await _context.TreatmentPlans
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.Treatments)
                .FirstOrDefaultAsync(p => p.Id == id && p.ClinicId == clinicId);

            if (plan == null)
                return NotFound();

            // حساب الإحصائيات
            var totalTreatments = plan.Treatments.Count;
            var completedTreatments = plan.Treatments.Count(t => t.Status == TreatmentStatus.Completed);
            var totalActualCost = plan.Treatments.Where(t => t.Status == TreatmentStatus.Completed).Sum(t => t.Cost);

            var vm = new TreatmentPlanDetailsViewModel
            {
                Plan = plan,
                TotalTreatments = totalTreatments,
                CompletedTreatments = completedTreatments,
                ProgressPercentage = totalTreatments > 0 ? (completedTreatments * 100 / totalTreatments) : 0,
                TotalActualCost = totalActualCost
            };

            ViewData["Title"] = plan.Title;
            return View(vm);
        }

        // 🟩 POST: /TreatmentPlan/AddTreatment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTreatment(
            int planId,
            int? toothNumber,
            TreatmentType type,
            string title,
            string? description,
            decimal estimatedCost,
            int priority)
        {
            var clinicId = GetCurrentClinicId();
            var doctorId = GetCurrentUserId();

            var plan = await _context.TreatmentPlans
                .FirstOrDefaultAsync(p => p.Id == planId && p.ClinicId == clinicId);

            if (plan == null)
                return NotFound();

            var treatment = new Treatment
            {
                Title = title,
                Description = description,
                Type = type,
                ToothNumber = toothNumber,
                EstimatedCost = estimatedCost,
                Cost = 0,
                Priority = priority,
                Status = TreatmentStatus.Planned,
                TreatmentPlanId = planId,
                PatientId = plan.PatientId,
                DoctorId = doctorId,
                ClinicId = clinicId,
                TreatmentDate = DateTime.UtcNow
            };

            _context.Treatments.Add(treatment);

            // تحديث التكلفة الإجمالية للخطة
            plan.TotalEstimatedCost += estimatedCost;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = planId });
        }

        // 🟡 POST: /TreatmentPlan/UpdateTreatment
       [HttpPost]
       [ValidateAntiForgeryToken]
       public async Task<IActionResult> UpdateTreatment(
       int treatmentId,
       TreatmentStatus status,
       decimal? actualCost,
       string? notes,
       IFormFile? beforeImageFile,
       IFormFile? afterImageFile)
       {
         var clinicId = GetCurrentClinicId();

         var treatment = await _context.Treatments
         .Include(t => t.TreatmentPlan)
         .FirstOrDefaultAsync(t => t.Id == treatmentId && t.ClinicId == clinicId);

        if (treatment == null)
          return NotFound();

    // تحديث الحالة
         treatment.Status = status;

    // تحديث التكلفة الفعلية
        if (actualCost.HasValue)
          treatment.Cost = actualCost.Value;
  
    // تحديث الملاحظات (نستخدمها كتفاصيل إضافية)
        if (!string.IsNullOrWhiteSpace(notes))
         treatment.Description = notes;

    // 🔹 رفع صورة "قبل" إن وُجدت
       if (beforeImageFile != null && beforeImageFile.Length > 0)
         {
           var url = await _cloudinary.UploadImageAsync(beforeImageFile);
           treatment.BeforeImageUrl = url;
         }

        // 🔹 رفع صورة "بعد" إن وُجدت
          if (afterImageFile != null && afterImageFile.Length > 0)
         {
           var url = await _cloudinary.UploadImageAsync(afterImageFile);
           treatment.AfterImageUrl = url;
         }

    // لو اكتمل العلاج نحفظ وقت الإكمال
         if (status == TreatmentStatus.Completed)
          treatment.CompletedAt = DateTime.UtcNow;

         await _context.SaveChangesAsync();

          return RedirectToAction(nameof(Details), new { id = treatment.TreatmentPlanId });
      }

        // 🔴 POST: /TreatmentPlan/DeleteTreatment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTreatment(int treatmentId)
        {
            var clinicId = GetCurrentClinicId();

            var treatment = await _context.Treatments
                .Include(t => t.TreatmentPlan)
                .FirstOrDefaultAsync(t => t.Id == treatmentId && t.ClinicId == clinicId);

            if (treatment == null)
                return NotFound();

            var planId = treatment.TreatmentPlanId;

            // تحديث التكلفة الإجمالية
            if (treatment.TreatmentPlan != null && treatment.EstimatedCost.HasValue)
            {
                treatment.TreatmentPlan.TotalEstimatedCost -= treatment.EstimatedCost.Value;
            }

            _context.Treatments.Remove(treatment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = planId });
        }

        // 🟡 POST: /TreatmentPlan/UpdatePlanStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePlanStatus(int planId, PlanStatus status)
        {
            var clinicId = GetCurrentClinicId();

            var plan = await _context.TreatmentPlans
                .FirstOrDefaultAsync(p => p.Id == planId && p.ClinicId == clinicId);

            if (plan == null)
                return NotFound();

            plan.Status = status;

            if (status == PlanStatus.Completed)
                plan.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = planId });
        }

        // 🔴 POST: /TreatmentPlan/DeletePlan
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeletePlan(int planId)
        {
            var clinicId = GetCurrentClinicId();

            var plan = await _context.TreatmentPlans
                .Include(p => p.Treatments)
                .FirstOrDefaultAsync(p => p.Id == planId && p.ClinicId == clinicId);

            if (plan == null)
                return NotFound();

            var patientId = plan.PatientId;

            _context.TreatmentPlans.Remove(plan);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { id = patientId });
        }
    }
}