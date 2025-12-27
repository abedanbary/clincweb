using ClinicApp.Web.Data;
using ClinicApp.Web.Models;
using ClinicApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Web.Controllers
{
    [Authorize(Roles = "Manager,Doctor,Assistant")]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientsController(ApplicationDbContext context)
        {
            _context = context;
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
                Teeth = patient.Teeth.ToList()
            };

            ViewData["Title"] = $"{patient.FirstName} {patient.LastName} - Profile";
            return View(vm);
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