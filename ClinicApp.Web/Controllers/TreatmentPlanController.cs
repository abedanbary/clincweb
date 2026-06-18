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
        private readonly IToothStatusService _toothStatus;

        public TreatmentPlanController(ApplicationDbContext context, ICloudinaryService cloudinary, IToothStatusService toothStatus)
        {
            _context = context;
            _cloudinary = cloudinary;
            _toothStatus = toothStatus;
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

            if (toothNumber.HasValue)
                await _toothStatus.SyncToothAsync(plan.PatientId, toothNumber.Value, clinicId);

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
  
        // Always update notes so the user can also clear them
        treatment.Description = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

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

         var toothNum = treatment.ToothNumber;
         var patientIdForSync = treatment.PatientId;
         var planIdForRedirect = treatment.TreatmentPlanId ?? 0;

         await _context.SaveChangesAsync();

         if (toothNum.HasValue)
             await _toothStatus.SyncToothAsync(patientIdForSync, toothNum.Value, clinicId);

          return RedirectToAction(nameof(Details), new { id = planIdForRedirect });
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
            var toothNum = treatment.ToothNumber;
            var patientIdForSync = treatment.PatientId;

            // تحديث التكلفة الإجمالية
            if (treatment.TreatmentPlan != null && treatment.EstimatedCost.HasValue)
            {
                treatment.TreatmentPlan.TotalEstimatedCost -= treatment.EstimatedCost.Value;
            }

            _context.Treatments.Remove(treatment);
            await _context.SaveChangesAsync();

            if (toothNum.HasValue)
                await _toothStatus.SyncToothAsync(patientIdForSync, toothNum.Value, clinicId);

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

        // 🟩 POST: /TreatmentPlan/QuickAddTreatment (AJAX — from dental chart)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickAddTreatment(
            int patientId, int toothNumber, TreatmentType type,
            string title, decimal estimatedCost, int priority)
        {
            var clinicId = GetCurrentClinicId();
            var doctorId = GetCurrentUserId();

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == patientId && p.ClinicId == clinicId);
            if (patient == null) return NotFound();

            // Find the most recent active plan or create one
            var plan = await _context.TreatmentPlans
                .Where(p => p.PatientId == patientId && p.ClinicId == clinicId && p.Status == PlanStatus.Active)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (plan == null)
            {
                plan = new TreatmentPlan
                {
                    Title = $"Treatment Plan — {DateTime.UtcNow:MMM yyyy}",
                    PatientId = patientId,
                    DoctorId = doctorId,
                    ClinicId = clinicId,
                    Status = PlanStatus.Active,
                    TotalEstimatedCost = 0,
                    CreatedAt = DateTime.UtcNow
                };
                _context.TreatmentPlans.Add(plan);
                await _context.SaveChangesAsync();
            }

            var treatment = new Treatment
            {
                Title = title,
                Type = type,
                ToothNumber = toothNumber,
                EstimatedCost = estimatedCost,
                Cost = 0,
                Priority = priority,
                Status = TreatmentStatus.Planned,
                TreatmentPlanId = plan.Id,
                PatientId = patientId,
                DoctorId = doctorId,
                ClinicId = clinicId,
                TreatmentDate = DateTime.UtcNow
            };
            _context.Treatments.Add(treatment);
            plan.TotalEstimatedCost += estimatedCost;
            await _context.SaveChangesAsync();

            await _toothStatus.SyncToothAsync(patientId, toothNumber, clinicId);

            var tooth = await _context.PatientTeeth
                .FirstOrDefaultAsync(t => t.PatientId == patientId && t.ToothNumber == toothNumber);

            return Json(new
            {
                success = true,
                treatmentId = treatment.Id,
                planId = plan.Id,
                toothStatus = tooth != null ? (int)tooth.Status : (int)ToothStatus.PlannedTreatment
            });
        }

        // 🟡 POST: /TreatmentPlan/QuickUpdateStatus (AJAX — from dental chart tooth panel)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickUpdateStatus(int treatmentId, TreatmentStatus status)
        {
            var clinicId = GetCurrentClinicId();

            var treatment = await _context.Treatments
                .FirstOrDefaultAsync(t => t.Id == treatmentId && t.ClinicId == clinicId);
            if (treatment == null) return NotFound();

            treatment.Status = status;
            if (status == TreatmentStatus.Completed)
                treatment.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            int newToothStatus = -1;
            if (treatment.ToothNumber.HasValue)
            {
                await _toothStatus.SyncToothAsync(treatment.PatientId, treatment.ToothNumber.Value, clinicId);
                var tooth = await _context.PatientTeeth
                    .FirstOrDefaultAsync(t => t.PatientId == treatment.PatientId && t.ToothNumber == treatment.ToothNumber.Value);
                newToothStatus = tooth != null ? (int)tooth.Status : -1;
            }

            return Json(new { success = true, toothStatus = newToothStatus });
        }

        // 🔴 POST: /TreatmentPlan/DeletePlan
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles ="Doctor")]
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