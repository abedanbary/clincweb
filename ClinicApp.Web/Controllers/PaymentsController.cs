using System.Security.Claims;
using ClinicApp.Web.Data;
using ClinicApp.Web.Models;
using ClinicApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Web.Controllers
{
    [Authorize(Roles = "Manager,Doctor")]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Payments
        public async Task<IActionResult> Index(int? patientId, PaymentStatus? status, DateTime? from, DateTime? to)
        {
            var clinicId = GetCurrentClinicId();
            var userRole = GetCurrentUserRole();
            var currentUserId = GetCurrentUserId();

            IQueryable<Payment> query = _context.Payments
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.Treatment)
                .Where(p => p.ClinicId == clinicId);

            if (userRole == UserRole.Doctor)
                query = query.Where(p => p.DoctorId == currentUserId);

            if (patientId.HasValue)
                query = query.Where(p => p.PatientId == patientId.Value);
            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);
            if (from.HasValue)
                query = query.Where(p => p.PaymentDate >= DateTime.SpecifyKind(from.Value, DateTimeKind.Utc));
            if (to.HasValue)
                query = query.Where(p => p.PaymentDate <= DateTime.SpecifyKind(to.Value.AddDays(1), DateTimeKind.Utc));

            var payments = await query.OrderByDescending(p => p.PaymentDate).ToListAsync();

            IQueryable<Payment> statsQuery = _context.Payments.Where(p => p.ClinicId == clinicId);
            if (userRole == UserRole.Doctor)
                statsQuery = statsQuery.Where(p => p.DoctorId == currentUserId);

            var allPayments = await statsQuery.ToListAsync();

            var vm = new PaymentsPageViewModel
            {
                Payments = payments,
                FilterPatientId = patientId,
                FilterStatus = status,
                FilterFrom = from,
                FilterTo = to,
                TotalRevenue = allPayments.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.Amount),
                PendingAmount = allPayments.Where(p => p.Status == PaymentStatus.Pending).Sum(p => p.Amount),
                RefundedAmount = allPayments.Where(p => p.Status == PaymentStatus.Refunded).Sum(p => p.Amount),
                PaidCount = allPayments.Count(p => p.Status == PaymentStatus.Paid),
                PendingCount = allPayments.Count(p => p.Status == PaymentStatus.Pending),
                NewPayment = new CreatePaymentViewModel { PaymentDate = DateTime.Today }
            };

            vm.FilterPatients = await _context.Patients
                .Where(p => p.ClinicId == clinicId)
                .OrderBy(p => p.FirstName)
                .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = $"{p.FirstName} {p.LastName}" })
                .ToListAsync();

            if (userRole == UserRole.Manager)
            {
                vm.Patients = await _context.Patients
                    .Where(p => p.ClinicId == clinicId)
                    .OrderBy(p => p.FirstName)
                    .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = $"{p.FirstName} {p.LastName} - {p.Phone}" })
                    .ToListAsync();

                vm.Doctors = await _context.AppUsers
                    .Where(u => u.ClinicId == clinicId && u.Role == UserRole.Doctor)
                    .OrderBy(u => u.FirstName)
                    .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = $"Dr. {u.FirstName} {u.LastName}" })
                    .ToListAsync();
            }

            ViewData["Title"] = "Payments";
            return View(vm);
        }

        // GET: /Payments/GetPatientTreatments?patientId=5
        [HttpGet]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetPatientTreatments(int patientId)
        {
            var clinicId = GetCurrentClinicId();
            var treatments = await _context.Treatments
                .Where(t => t.PatientId == patientId && t.ClinicId == clinicId)
                .OrderByDescending(t => t.TreatmentDate)
                .Select(t => new
                {
                    value = t.Id,
                    text = $"{t.Title} ({t.Type}) - ${t.Cost:F2}",
                    cost = t.Cost,
                    doctorId = t.DoctorId
                })
                .ToListAsync();

            return Json(treatments);
        }

        // GET: /Payments/GetPayment/5
        [HttpGet]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetPayment(int id)
        {
            var clinicId = GetCurrentClinicId();
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == id && p.ClinicId == clinicId);

            if (payment == null)
                return NotFound();

            return Json(new
            {
                id = payment.Id,
                patientId = payment.PatientId,
                doctorId = payment.DoctorId,
                treatmentId = payment.TreatmentId,
                amount = payment.Amount,
                method = (int)payment.Method,
                status = (int)payment.Status,
                paymentDate = payment.PaymentDate.ToString("yyyy-MM-dd"),
                notes = payment.Notes
            });
        }

        // POST: /Payments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create(PaymentsPageViewModel model)
        {
            var clinicId = GetCurrentClinicId();
            var input = model.NewPayment;

            if (input.Amount <= 0 || input.PatientId == 0 || input.DoctorId == 0)
            {
                TempData["Error"] = "Please fill all required fields correctly.";
                return RedirectToAction(nameof(Index));
            }

            var lastInvoice = await _context.Payments
                .Where(p => p.ClinicId == clinicId)
                .MaxAsync(p => (int?)p.InvoiceNumber) ?? 1000;

            var payment = new Payment
            {
                InvoiceNumber = lastInvoice + 1,
                PatientId = input.PatientId,
                DoctorId = input.DoctorId,
                TreatmentId = input.TreatmentId == 0 ? null : input.TreatmentId,
                Amount = input.Amount,
                Method = input.Method,
                Status = input.Status,
                PaymentDate = DateTime.SpecifyKind(input.PaymentDate, DateTimeKind.Utc),
                Notes = input.Notes,
                ClinicId = clinicId
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Payment #{payment.InvoiceNumber} created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Payments/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(EditPaymentViewModel model)
        {
            var clinicId = GetCurrentClinicId();

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == model.Id && p.ClinicId == clinicId);

            if (payment == null)
            {
                TempData["Error"] = "Payment not found.";
                return RedirectToAction(nameof(Index));
            }

            payment.PatientId = model.PatientId;
            payment.DoctorId = model.DoctorId;
            payment.TreatmentId = model.TreatmentId == 0 ? null : model.TreatmentId;
            payment.Amount = model.Amount;
            payment.Method = model.Method;
            payment.Status = model.Status;
            payment.PaymentDate = DateTime.SpecifyKind(model.PaymentDate, DateTimeKind.Utc);
            payment.Notes = model.Notes;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Payments/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var clinicId = GetCurrentClinicId();

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == id && p.ClinicId == clinicId);

            if (payment == null)
            {
                TempData["Error"] = "Payment not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private int GetCurrentClinicId() =>
            int.Parse(User.FindFirstValue("ClinicId")!);

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private UserRole GetCurrentUserRole()
        {
            var roleString = User.FindFirstValue(ClaimTypes.Role);
            return Enum.Parse<UserRole>(roleString!);
        }
    }
}
