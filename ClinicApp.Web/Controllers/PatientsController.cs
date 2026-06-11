using ClinicApp.Web.Data;
using ClinicApp.Web.Models;
using ClinicApp.Web.ViewModels;
using ClinicApp.Web.Services.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Web.Controllers
{
    [Authorize(Roles = "Manager,Doctor,Assistant")]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _storage;

        public PatientsController(ApplicationDbContext context, IFileStorageService storage)
        {
            _context = context;
            _storage = storage;
        }

        private int GetCurrentClinicId()
        {
            var clinicClaim = User.FindFirst("ClinicId");
            if (clinicClaim == null)
                throw new InvalidOperationException("ClinicId claim is missing.");

            return int.Parse(clinicClaim.Value);
        }

        // 🟦 GET: /Patients
        public async Task<IActionResult> Index()
        {
            var clinicId = GetCurrentClinicId();
            var patients = await _context.Patients
                .Where(p => p.ClinicId == clinicId)
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();

            var vm = new PatientsPageViewModel
            {
                Patients = patients,
                NewPatient = new CreatePatientViewModel()
            };

            ViewData["Title"] = "Patients";
            return View(vm);
        }

        // 🟩 POST: /Patients/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(PatientsPageViewModel vm)
        {
            var clinicId = GetCurrentClinicId();

            if (!ModelState.IsValid)
            {
                vm.Patients = await _context.Patients
                    .Where(p => p.ClinicId == clinicId)
                    .OrderBy(p => p.LastName)
                    .ThenBy(p => p.FirstName)
                    .ToListAsync();

                return View("Index", vm);
            }

            // Map ViewModel to Entity
            var newPatient = new Patient
            {
                IdNumber = vm.NewPatient.IdNumber,
                FirstName = vm.NewPatient.FirstName,
                LastName = vm.NewPatient.LastName,
                Phone = vm.NewPatient.Phone,
                Email = vm.NewPatient.Email,
                DateOfBirth = DateTime.SpecifyKind(vm.NewPatient.DateOfBirth, DateTimeKind.Utc),  // ✅ هنا التغيير
                Gender = vm.NewPatient.Gender,
                Address = vm.NewPatient.Address,
                MedicalNotes = vm.NewPatient.MedicalNotes,
                Allergies = vm.NewPatient.Allergies,
                ChronicDiseases = vm.NewPatient.ChronicDiseases,
                ClinicId = clinicId
            };

            _context.Patients.Add(newPatient);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // 🟡 POST: /Patients/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            string? idNumber,
            string firstName,
            string lastName,
            string phone,
            string? email,
            DateTime dateOfBirth,
            string gender,
            string? address,
            string? medicalNotes,
            string? allergies,
            string? chronicDiseases)
        {
            var clinicId = GetCurrentClinicId();

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == id && p.ClinicId == clinicId);

            if (patient == null)
                return NotFound();

            patient.IdNumber = idNumber;
            patient.FirstName = firstName;
            patient.LastName = lastName;
            patient.Phone = phone;
            patient.Email = email;
            patient.DateOfBirth = DateTime.SpecifyKind(dateOfBirth, DateTimeKind.Utc);
            patient.Gender = gender;
            patient.Address = address;
            patient.MedicalNotes = medicalNotes;
            patient.Allergies = allergies;
            patient.ChronicDiseases = chronicDiseases;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // 🔴 POST: /Patients/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var clinicId = GetCurrentClinicId();

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == id && p.ClinicId == clinicId);

            if (patient == null)
                return NotFound();

            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

      [Authorize(Roles ="Doctor")]
        public async Task<IActionResult> Profile(int id)
        {
            var clinicId = GetCurrentClinicId();

            var patient = await _context.Patients
                .Include(p => p.Teeth)
                .Include(p => p.Files)
                .FirstOrDefaultAsync(p => p.Id == id && p.ClinicId == clinicId);

            if (patient == null)
                return NotFound();

            var vm = new PatientProfileViewModel
            {
                PatientId = patient.Id,
                IdNumber = patient.IdNumber,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Phone = patient.Phone,
                Email = patient.Email,
                DateOfBirth = patient.DateOfBirth,
                Age = DateTime.Now.Year - patient.DateOfBirth.Year,
                Gender = patient.Gender,
                Address = patient.Address,
                MedicalNotes = patient.MedicalNotes,
                Allergies = patient.Allergies,
                ChronicDiseases = patient.ChronicDiseases,
                Teeth = patient.Teeth.ToList(),
                Files = patient.Files.OrderByDescending(f => f.UploadedAtUtc).ToList()
            };

            ViewData["Title"] = $"{patient.FirstName} {patient.LastName} - Profile";
            return View(vm);
        }

        // POST: /Patients/UploadFile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doctor")]
        [EnableRateLimiting("upload-limit")]
        public async Task<IActionResult> UploadFile(int patientId, IFormFile file, string? description, CancellationToken cancellationToken)
        {
            var clinicId = GetCurrentClinicId();

            var patientExists = await _context.Patients
                .AnyAsync(p => p.Id == patientId && p.ClinicId == clinicId, cancellationToken);

            if (!patientExists)
                return NotFound();

            try
            {
                var result = await _storage.UploadAsync(file, $"patients/{patientId}", cancellationToken);

                var patientFile = new PatientFile
                {
                    PatientId = patientId,
                    OriginalFileName = result.OriginalFileName,
                    ObjectPath = result.ObjectPath,
                    ContentType = result.ContentType,
                    Size = result.Size,
                    UploadedAtUtc = result.UploadedAtUtc,
                    Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
                };

                _context.PatientFiles.Add(patientFile);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (ArgumentException ex)
            {
                TempData["FileError"] = ex.Message;
            }

            return RedirectToAction(nameof(Profile), new { id = patientId });
        }

        // GET: /Patients/FileUrl?fileId=X  (returns JSON { url })
        [HttpGet]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> FileUrl(int fileId, CancellationToken cancellationToken)
        {
            var clinicId = GetCurrentClinicId();

            var file = await _context.PatientFiles
                .Include(f => f.Patient)
                .FirstOrDefaultAsync(f => f.Id == fileId && f.Patient.ClinicId == clinicId, cancellationToken);

            if (file == null)
                return NotFound();

            var url = await _storage.GenerateSignedReadUrlAsync(file.ObjectPath, TimeSpan.FromMinutes(30), cancellationToken);
            return Ok(new { url });
        }

        // POST: /Patients/DeleteFile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> DeleteFile(int fileId, int patientId, CancellationToken cancellationToken)
        {
            var clinicId = GetCurrentClinicId();

            var file = await _context.PatientFiles
                .Include(f => f.Patient)
                .FirstOrDefaultAsync(f => f.Id == fileId && f.Patient.ClinicId == clinicId, cancellationToken);

            if (file == null)
                return NotFound();

            try
            {
                await _storage.DeleteAsync(file.ObjectPath, cancellationToken);
            }
            catch { /* file may already be gone in GCS — still remove DB record */ }

            _context.PatientFiles.Remove(file);
            await _context.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Profile), new { id = patientId });
        }

        // 🟩 POST: /Patients/UpdateTooth
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTooth(int patientId, int toothNumber, ToothStatus status, string? notes)
        {
            var clinicId = GetCurrentClinicId();

            var patientExists = await _context.Patients
                .AnyAsync(p => p.Id == patientId && p.ClinicId == clinicId);

            if (!patientExists)
                return NotFound();

            var tooth = await _context.PatientTeeth
                .FirstOrDefaultAsync(t => t.PatientId == patientId && t.ToothNumber == toothNumber);

            if (tooth == null)
            {
                tooth = new PatientTooth
                {
                    PatientId = patientId,
                    ToothNumber = toothNumber,
                    Status = status,
                    Notes = notes,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.PatientTeeth.Add(tooth);
            }
            else
            {
                tooth.Status = status;
                tooth.Notes = notes;
                tooth.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, toothNumber, status = status.ToString() });
        }
    }
}