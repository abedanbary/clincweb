using ClinicApp.Web.Data;
using ClinicApp.Web.Models;
using ClinicApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

using ClinicApp.Web.Services;

namespace ClinicApp.Web.Controllers
{
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPrintService _printService;
        private readonly IExportService _exportService;

        public AppointmentsController(ApplicationDbContext context,IPrintService printService,IExportService exportService)
        {
            _context = context;
            _printService=printService;
            _exportService=exportService;
        }

        // GET: Appointments (Index with form)
        public async Task<IActionResult> Index()
        {
            var clinicId = GetCurrentClinicId();
            var userRole = GetCurrentUserRole();
            var currentUserId = GetCurrentUserId();

            IQueryable<Appointment> appointmentsQuery = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.ClinicId == clinicId);

            // إذا دكتور → يشوف مواعيده فقط
            if (userRole == UserRole.Doctor)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.DoctorId == currentUserId);
            }

            var appointments = await appointmentsQuery
                .OrderBy(a => a.StartTime)
                .ToListAsync();

            var model = new AppointmentsPageViewModel
            {
                Appointments = appointments,
                NewAppointment = new CreateAppointmentViewModel(),
                Patients = await GetPatientsListAsyncInternal(),
                Doctors = await GetDoctorsListAsyncInternal() 
            };

            return View(model);
        }

        // GET: Calendar View
       // GET: Calendar View
        public async Task<IActionResult> Calendar()
        {
            var clinicId = GetCurrentClinicId();
            var userRole = GetCurrentUserRole();
            var currentUserId = GetCurrentUserId();

           IQueryable<Appointment> appointmentsQuery = _context.Appointments
              .Include(a => a.Patient)
              .Include(a => a.Doctor)
              .Where(a => a.ClinicId == clinicId);

    // إذا دكتور → يشوف مواعيده فقط
           if (userRole == UserRole.Doctor)
             {
              appointmentsQuery = appointmentsQuery.Where(a => a.DoctorId == currentUserId);
             }

             var appointments = await appointmentsQuery
              .OrderBy(a => a.StartTime)
              .ToListAsync();

            var model = new AppointmentsPageViewModel
              {
               Appointments = appointments,
               NewAppointment = new CreateAppointmentViewModel(),
               Patients = await GetPatientsListAsyncInternal(),
               Doctors = await GetDoctorsListAsyncInternal()
              };

            return View(model);
        } 
        // GET: Appointments as JSON for calendar
        [HttpGet]
        public async Task<IActionResult> GetCalendarEvents()
        {
            var clinicId = GetCurrentClinicId();
            var userRole = GetCurrentUserRole();
            var currentUserId = GetCurrentUserId();

            IQueryable<Appointment> appointmentsQuery = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.ClinicId == clinicId);

            if (userRole == UserRole.Doctor)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.DoctorId == currentUserId);
            }

            var appointments = await appointmentsQuery.ToListAsync();

            var events = appointments.Select(a => new
            {
                id = a.Id,
                title = $"{a.Patient.FirstName} {a.Patient.LastName}",
                start = a.StartTime.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss"),
                end = a.EndTime.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss"),
                color = a.Status switch
                {
                    AppointmentStatus.Scheduled => "#3b82f6",
                    AppointmentStatus.Completed => "#10b981",
                    AppointmentStatus.Cancelled => "#ef4444",
                    AppointmentStatus.NoShow => "#f59e0b",
                    _ => "#3b82f6"
                },
                extendedProps = new
                {
                    patientName = $"{a.Patient.FirstName} {a.Patient.LastName}",
                    patientPhone = a.Patient.Phone,
                    doctorName = $"Dr. {a.Doctor.FirstName} {a.Doctor.LastName}",
                    reason = a.ReasonForVisit,
                    status = a.Status.ToString()
                }
            });

            return Json(events);
        }

        // GET: Get single appointment
        [HttpGet]
        public async Task<IActionResult> GetAppointment(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            var result = new
            {
                id = appointment.Id,
                patientId = appointment.PatientId,
                doctorId = appointment.DoctorId,
                startTime = appointment.StartTime.ToLocalTime().ToString("yyyy-MM-ddTHH:mm"),
                endTime = appointment.EndTime.ToLocalTime().ToString("yyyy-MM-ddTHH:mm"),
                reasonForVisit = appointment.ReasonForVisit,
                notes = appointment.Notes,
                status = (int)appointment.Status,
                patientName = $"{appointment.Patient.FirstName} {appointment.Patient.LastName}",
                doctorName = $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}"
            };

            return Json(result);
        }

        // API: Get Doctors List as JSON
        [HttpGet]
        public async Task<IActionResult> GetDoctorsListAsync()
        {
            var doctors = await GetDoctorsListAsyncInternal();
            return Json(doctors);
        }

        // API: Get Patients List as JSON
        [HttpGet]
        public async Task<IActionResult> GetPatientsListAsync()
        {
            var patients = await GetPatientsListAsyncInternal();
            return Json(patients);
        }

        // POST: Appointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppointmentsPageViewModel model)
        {
            var appointmentModel = model.NewAppointment;

            if (!ModelState.IsValid)
            {
                // إعادة تحميل البيانات
                var clinicId = GetCurrentClinicId();
                var userRole = GetCurrentUserRole();
                var currentUserId = GetCurrentUserId();

                IQueryable<Appointment> appointmentsQuery = _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Where(a => a.ClinicId == clinicId);

                if (userRole == UserRole.Doctor)
                {
                    appointmentsQuery = appointmentsQuery.Where(a => a.DoctorId == currentUserId);
                }

                model.Appointments = await appointmentsQuery.OrderBy(a => a.StartTime).ToListAsync();
                model.Patients = await GetPatientsListAsyncInternal();
                model.Doctors = await GetDoctorsListAsyncInternal();
                
                return View("Index", model);
            }

            var clinicIdForCreate = GetCurrentClinicId();
            var currentUserIdForCreate = GetCurrentUserId();

            int patientId;

            // إذا مريض جديد
            if (appointmentModel.IsNewPatient)
            {
                if (string.IsNullOrWhiteSpace(appointmentModel.NewPatientFirstName) ||
                    string.IsNullOrWhiteSpace(appointmentModel.NewPatientLastName) ||
                    string.IsNullOrWhiteSpace(appointmentModel.NewPatientPhone))
                {
                    TempData["Error"] = "Please fill all required patient fields";
                    return RedirectToAction(nameof(Index));
                }

                var newPatient = new Patient
                {
                    FirstName = appointmentModel.NewPatientFirstName!,
                    LastName = appointmentModel.NewPatientLastName!,
                    Phone = appointmentModel.NewPatientPhone!,
                    Email = appointmentModel.NewPatientEmail,
                    DateOfBirth = appointmentModel.NewPatientDateOfBirth.HasValue 
                        ? DateTime.SpecifyKind(appointmentModel.NewPatientDateOfBirth.Value, DateTimeKind.Utc)
                        : DateTime.UtcNow.AddYears(-30),
                    Gender = appointmentModel.NewPatientGender ?? "Male",
                    Address = appointmentModel.NewPatientAddress,
                    MedicalNotes = appointmentModel.NewPatientMedicalHistory,
                    ClinicId = clinicIdForCreate
                };

                _context.Patients.Add(newPatient);
                await _context.SaveChangesAsync();

                patientId = newPatient.Id;
            }
            else
            {
                if (!appointmentModel.ExistingPatientId.HasValue)
                {
                    TempData["Error"] = "Please select a patient";
                    return RedirectToAction(nameof(Index));
                }

                patientId = appointmentModel.ExistingPatientId.Value;
            }

            // تحويل الأوقات إلى UTC مرة واحدة
            var startTimeUtc = DateTime.SpecifyKind(appointmentModel.StartTime.Value, DateTimeKind.Utc);
            var endTimeUtc = DateTime.SpecifyKind(appointmentModel.EndTime.Value, DateTimeKind.Utc);
            // التحقق من التعارض
            var hasConflict = await _context.Appointments
                .AnyAsync(a => a.DoctorId == appointmentModel.DoctorId &&
                              a.StartTime < endTimeUtc &&
                              a.EndTime > startTimeUtc &&
                              a.Status != AppointmentStatus.Cancelled);

            if (hasConflict)
            {
                TempData["Error"] = "Doctor already has an appointment at this time";
                return RedirectToAction(nameof(Index));
            }

            // إنشاء الموعد
            var appointment = new Appointment
            {
                PatientId = patientId,
                DoctorId = appointmentModel.DoctorId,
                ClinicId = clinicIdForCreate,
                StartTime = startTimeUtc,
                EndTime = endTimeUtc,
                ReasonForVisit = appointmentModel.ReasonForVisit,
                Notes = appointmentModel.Notes,
                Status = AppointmentStatus.Scheduled,
                CreatedByUserId = currentUserIdForCreate,
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment created successfully!";
            
            // Check if request came from calendar
            var referer = Request.Headers["Referer"].ToString();
            if (referer.Contains("/Calendar"))
            {
                return RedirectToAction(nameof(Calendar));
            }
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Appointments/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditAppointmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid data provided";
                return RedirectToAction(nameof(Calendar));
            }

            var appointment = await _context.Appointments.FindAsync(model.Id);
            
            if (appointment == null)
            {
                TempData["Error"] = "Appointment not found";
                return RedirectToAction(nameof(Calendar));
            }

            // تحويل الأوقات إلى UTC
             var startTimeUtc = DateTime.SpecifyKind(model.StartTime.Value, DateTimeKind.Utc);
              var endTimeUtc = DateTime.SpecifyKind(model.EndTime.Value, DateTimeKind.Utc);

            // التحقق من التعارض (باستثناء الموعد الحالي)
            var hasConflict = await _context.Appointments
                .AnyAsync(a => a.Id != model.Id &&
                              a.DoctorId == model.DoctorId &&
                              a.StartTime < endTimeUtc &&
                              a.EndTime > startTimeUtc &&
                              a.Status != AppointmentStatus.Cancelled);

            if (hasConflict)
            {
                TempData["Error"] = "Doctor already has an appointment at this time";
                return RedirectToAction(nameof(Calendar));
            }

            // تحديث البيانات
            appointment.DoctorId = model.DoctorId;
            appointment.StartTime = startTimeUtc;
            appointment.EndTime = endTimeUtc;
            appointment.ReasonForVisit = model.ReasonForVisit;
            appointment.Notes = model.Notes;
            appointment.Status = model.Status;

            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment updated successfully!";
            return RedirectToAction(nameof(Calendar));
        }

        // POST: Appointments/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Appointment deleted successfully!";
            }

            // Check if request came from calendar
            var referer = Request.Headers["Referer"].ToString();
            if (referer.Contains("/Calendar"))
            {
                return RedirectToAction(nameof(Calendar));
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper Methods
        private int GetCurrentClinicId()
        {
            return int.Parse(User.FindFirstValue("ClinicId")!);
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        private UserRole GetCurrentUserRole()
        {
            var roleString = User.FindFirstValue(ClaimTypes.Role);
            return Enum.Parse<UserRole>(roleString!);
        }

        private async Task<List<SelectListItem>> GetPatientsListAsyncInternal()
        {
            var clinicId = GetCurrentClinicId();
            return await _context.Patients
                .Where(p => p.ClinicId == clinicId)
                .OrderBy(p => p.FirstName)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.FirstName} {p.LastName} - {p.Phone}"
                })
                .ToListAsync();
        }

    
        private async Task<List<SelectListItem>> GetDoctorsListAsyncInternal()
        {
            var clinicId = GetCurrentClinicId();
            return await _context.AppUsers
                .Where(u => u.ClinicId == clinicId && u.Role == UserRole.Doctor)
                .OrderBy(u => u.FirstName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = $"Dr. {u.FirstName} {u.LastName}"
                })
                .ToListAsync();
       }
        // GET: Print Week Schedule as PDF
       [HttpGet]
        public async Task<IActionResult> PrintWeekSchedulePdf(DateTime? weekStart)
        {
            var clinicId = GetCurrentClinicId();
            var userRole = GetCurrentUserRole();
            var currentUserId = GetCurrentUserId();

    // إذا ما في تاريخ، استخدم الأسبوع الحالي
            var startDate = weekStart ?? DateTime.Today;
             var startDateUtc = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
    // احسب بداية الأسبوع (Monday)
            var dayOfWeek = (int)startDateUtc.DayOfWeek;
            var weekStartDate =startDateUtc.AddDays(-(dayOfWeek == 0 ? 6 : dayOfWeek - 1));
            var weekEndDate = weekStartDate.AddDays(7);

    // جلب المواعيد
           IQueryable<Appointment> appointmentsQuery = _context.Appointments
               .Include(a => a.Patient)
               .Include(a => a.Doctor)
               .Where(a => a.ClinicId == clinicId &&
                   a.StartTime >= weekStartDate &&
                   a.StartTime < weekEndDate);

            if (userRole == UserRole.Doctor)
            {
              appointmentsQuery = appointmentsQuery.Where(a => a.DoctorId == currentUserId);
            }

              var appointments = await appointmentsQuery
              .OrderBy(a => a.StartTime)
              .ToListAsync();

    // Get clinic info
              var clinic = await _context.Clinics.FindAsync(clinicId);
              var clinicName = clinic?.Name ?? "Dental Clinic";

              // Generate PDF
              var pdfBytes = _printService.GenerateWeekSchedulePdf(weekStartDate, appointments, clinicName);

             // Return PDF file
              var fileName = $"Week_Schedule_{weekStartDate:yyyy-MM-dd}.pdf";
              return File(pdfBytes, "application/pdf", fileName);
        }



                // GET: Export Week Schedule to Excel
        [HttpGet]
        public async Task<IActionResult> ExportWeekScheduleExcel(DateTime? weekStart)
        {
           var clinicId = GetCurrentClinicId();
           var userRole = GetCurrentUserRole();
           var currentUserId = GetCurrentUserId();

           var startDate = weekStart ?? DateTime.Today;
           var startDateUtc = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
    
           var dayOfWeek = (int)startDateUtc.DayOfWeek;
           var weekStartDate = startDateUtc.AddDays(-(dayOfWeek == 0 ? 6 : dayOfWeek - 1));
           var weekEndDate = weekStartDate.AddDays(7);

           IQueryable<Appointment> appointmentsQuery = _context.Appointments
              .Include(a => a.Patient)
              .Include(a => a.Doctor)
              .Where(a => a.ClinicId == clinicId &&
                   a.StartTime >= weekStartDate &&
                   a.StartTime < weekEndDate);

            if (userRole == UserRole.Doctor)
            {
              appointmentsQuery = appointmentsQuery.Where(a => a.DoctorId == currentUserId);
            }

             var appointments = await appointmentsQuery
              .OrderBy(a => a.StartTime)
              .ToListAsync();

            var clinic = await _context.Clinics.FindAsync(clinicId);
            var clinicName = clinic?.Name ?? "Dental Clinic";

            // Generate Excel
            var excelBytes = _exportService.ExportWeekScheduleToExcel(weekStartDate, appointments, clinicName);

            var fileName = $"Week_Schedule_{weekStartDate:yyyy-MM-dd}.xlsx";
            return File(excelBytes, 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
           fileName);
        }


    }

}