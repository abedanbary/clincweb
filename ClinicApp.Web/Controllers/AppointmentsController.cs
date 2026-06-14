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

        // API: Get all doctors with schedule and conflict flags for the given time slot
        [HttpGet]
        public async Task<IActionResult> GetAvailableDoctors(string startTime, string endTime)
        {
            if (!DateTime.TryParse(startTime, out var start) || !DateTime.TryParse(endTime, out var end))
                return Json(new List<object>());

            var clinicId     = GetCurrentClinicId();
            var startUtc     = DateTime.SpecifyKind(start, DateTimeKind.Utc);
            var endUtc       = DateTime.SpecifyKind(end,   DateTimeKind.Utc);
            var dayOfWeek    = startUtc.DayOfWeek;
            var timeOfDay    = startUtc.TimeOfDay;
            var timeOfDayEnd = endUtc.TimeOfDay;

            var allDoctors = await _context.AppUsers
                .Where(u => u.ClinicId == clinicId && u.Role == UserRole.Doctor)
                .OrderBy(u => u.FirstName)
                .ToListAsync();

            var schedules = await _context.DoctorSchedules
                .Where(s => s.ClinicId == clinicId && s.DayOfWeek == dayOfWeek && s.IsWorkingDay)
                .ToListAsync();

            bool schedulesConfigured = await _context.DoctorSchedules.AnyAsync(s => s.ClinicId == clinicId);

            // Doctors whose schedule covers this time slot
            var scheduledDoctorIds = schedulesConfigured
                ? schedules
                    .Where(s => s.StartTime <= timeOfDay && s.EndTime >= timeOfDayEnd)
                    .Select(s => s.DoctorId)
                    .ToHashSet()
                : allDoctors.Select(d => d.Id).ToHashSet(); // no schedules configured → all work

            // Doctors with a conflicting appointment
            var busyDoctorIds = (await _context.Appointments
                .Where(a => a.ClinicId == clinicId &&
                            a.StartTime < endUtc &&
                            a.EndTime   > startUtc &&
                            a.Status    != AppointmentStatus.Cancelled)
                .Select(a => a.DoctorId)
                .Distinct()
                .ToListAsync()).ToHashSet();

            // Return ALL doctors with flags — UI decides how to display and warn
            var result = allDoctors.Select(d => new
            {
                value          = d.Id,
                text           = $"Dr. {d.FirstName} {d.LastName}",
                worksAtThisTime = scheduledDoctorIds.Contains(d.Id),
                hasConflict    = busyDoctorIds.Contains(d.Id)
            }).ToList();

            return Json(result);
        }

        // API: Find the first free 30-min slot for a doctor on or after a given date
        [HttpGet]
        public async Task<IActionResult> GetNextAvailableSlot(int doctorId, string? date = null)
        {
            var clinicId  = GetCurrentClinicId();
            var startDate = date != null && DateTime.TryParse(date, out var d)
                ? d.Date
                : DateTime.UtcNow.Date;

            // Never suggest slots in the past
            if (startDate < DateTime.UtcNow.Date)
                startDate = DateTime.UtcNow.Date;

            bool schedulesConfigured = await _context.DoctorSchedules.AnyAsync(s => s.ClinicId == clinicId);

            for (int dayOffset = 0; dayOffset < 14; dayOffset++)
            {
                var day = startDate.AddDays(dayOffset);
                var dayEnd = day.AddDays(1);

                // Get schedule for this doctor on this day
                TimeSpan workStart, workEnd;
                if (schedulesConfigured)
                {
                    var sched = await _context.DoctorSchedules.FirstOrDefaultAsync(
                        s => s.DoctorId == doctorId && s.ClinicId == clinicId
                          && s.DayOfWeek == day.DayOfWeek && s.IsWorkingDay);
                    if (sched == null) continue;  // doesn't work this day
                    workStart = sched.StartTime;
                    workEnd   = sched.EndTime;
                }
                else
                {
                    workStart = new TimeSpan(9, 0, 0);
                    workEnd   = new TimeSpan(17, 0, 0);
                }

                // Get booked slots for this doctor that day
                var booked = await _context.Appointments
                    .Where(a => a.DoctorId  == doctorId
                             && a.ClinicId  == clinicId
                             && a.StartTime >= day.ToUniversalTime()
                             && a.StartTime <  dayEnd.ToUniversalTime()
                             && a.Status    != AppointmentStatus.Cancelled)
                    .Select(a => new { a.StartTime, a.EndTime })
                    .ToListAsync();

                // Iterate in 30-min steps through working hours
                var cursor = workStart;
                while (cursor.Add(TimeSpan.FromMinutes(30)) <= workEnd)
                {
                    var slotStart = day.Add(cursor);
                    var slotEnd   = slotStart.AddMinutes(30);

                    // Skip past slots (same day only)
                    if (slotStart <= DateTime.UtcNow) { cursor = cursor.Add(TimeSpan.FromMinutes(30)); continue; }

                    var slotStartUtc = DateTime.SpecifyKind(slotStart, DateTimeKind.Utc);
                    var slotEndUtc   = DateTime.SpecifyKind(slotEnd,   DateTimeKind.Utc);

                    bool conflict = booked.Any(b => b.StartTime < slotEndUtc && b.EndTime > slotStartUtc);
                    if (!conflict)
                    {
                        var isToday    = day == DateTime.UtcNow.Date;
                        var dateLabel  = isToday ? "Today" : day.ToString("ddd, MMM dd");
                        var timeLabel  = $"{cursor.Hours:D2}:{cursor.Minutes:D2}";
                        return Json(new
                        {
                            found      = true,
                            startIso   = slotStartUtc.ToString("yyyy-MM-ddTHH:mm"),
                            endIso     = slotEndUtc.ToString("yyyy-MM-ddTHH:mm"),
                            dateIso    = day.ToString("yyyy-MM-dd"),
                            timeValue  = timeLabel,
                            label      = $"{dateLabel} at {timeLabel}"
                        });
                    }
                    cursor = cursor.Add(TimeSpan.FromMinutes(30));
                }
            }

            return Json(new { found = false });
        }

        // API: Get Patients List as JSON
        [HttpGet]
        public async Task<IActionResult> GetPatientsListAsync()
        {
            var patients = await GetPatientsListAsyncInternal();
            return Json(patients);
        }

        // API: Check if a patient has a conflicting appointment at the given time
        [HttpGet]
        public async Task<IActionResult> CheckPatientConflict(int patientId, string startTime, string endTime, int? excludeId = null)
        {
            if (!DateTime.TryParse(startTime, out var start) || !DateTime.TryParse(endTime, out var end))
                return Json(new { hasConflict = false });

            var startUtc = DateTime.SpecifyKind(start, DateTimeKind.Utc);
            var endUtc   = DateTime.SpecifyKind(end,   DateTimeKind.Utc);

            var conflict = await _context.Appointments
                .Where(a => a.PatientId == patientId &&
                            a.StartTime < endUtc &&
                            a.EndTime   > startUtc &&
                            a.Status    != AppointmentStatus.Cancelled &&
                            (excludeId == null || a.Id != excludeId))
                .Select(a => new { a.StartTime, a.EndTime })
                .FirstOrDefaultAsync();

            if (conflict == null)
                return Json(new { hasConflict = false });

            var localStart = conflict.StartTime.ToLocalTime().ToString("ddd dd MMM, HH:mm");
            var localEnd   = conflict.EndTime.ToLocalTime().ToString("HH:mm");
            return Json(new { hasConflict = true, message = $"Patient already has an appointment {localStart}–{localEnd}" });
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
            var userRoleForCreate = GetCurrentUserRole();

            // Doctors can only book for themselves
            if (userRoleForCreate == UserRole.Doctor)
            {
                appointmentModel.DoctorId = currentUserIdForCreate;
            }

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

            // Block past appointments (allow up to 5 min in the past for clock drift)
            if (startTimeUtc < DateTime.UtcNow.AddMinutes(-5))
            {
                TempData["Error"] = "Cannot create appointments in the past";
                var referer0 = Request.Headers["Referer"].ToString();
                return referer0.Contains("/Calendar") ? RedirectToAction(nameof(Calendar)) : RedirectToAction(nameof(Index));
            }

            // التحقق من التعارض
            var hasConflict = await _context.Appointments
                .AnyAsync(a => a.DoctorId == appointmentModel.DoctorId &&
                              a.StartTime < endTimeUtc &&
                              a.EndTime > startTimeUtc &&
                              a.Status != AppointmentStatus.Cancelled);

            if (hasConflict)
            {
                TempData["Error"] = "Doctor already has an appointment at this time";
                var referer = Request.Headers["Referer"].ToString();
                return referer.Contains("/Calendar") ? RedirectToAction(nameof(Calendar)) : RedirectToAction(nameof(Index));
            }

            // Prevent same patient from having two overlapping appointments
            var hasPatientConflict = await _context.Appointments
                .AnyAsync(a => a.PatientId == patientId &&
                               a.StartTime < endTimeUtc &&
                               a.EndTime > startTimeUtc &&
                               a.Status != AppointmentStatus.Cancelled);

            if (hasPatientConflict)
            {
                TempData["Error"] = "This patient already has an appointment at this time";
                var referer2 = Request.Headers["Referer"].ToString();
                return referer2.Contains("/Calendar") ? RedirectToAction(nameof(Calendar)) : RedirectToAction(nameof(Index));
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
            var referer3 = Request.Headers["Referer"].ToString();
            return referer3.Contains("/Calendar") ? RedirectToAction(nameof(Calendar)) : RedirectToAction(nameof(Index));
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

            // Prevent same patient from having two overlapping appointments
            var hasPatientConflictEdit = await _context.Appointments
                .AnyAsync(a => a.Id != model.Id &&
                               a.PatientId == appointment.PatientId &&
                               a.StartTime < endTimeUtc &&
                               a.EndTime > startTimeUtc &&
                               a.Status != AppointmentStatus.Cancelled);

            if (hasPatientConflictEdit)
            {
                TempData["Error"] = "This patient already has an appointment at this time";
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