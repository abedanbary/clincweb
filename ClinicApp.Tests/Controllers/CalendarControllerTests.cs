using ClinicApp.Web.Models;
using ClinicApp.Web.Controllers;
using ClinicApp.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace ClinicApp.Tests.Controllers;

public class CalendarControllerTests
{
    // ── Seed helpers ─────────────────────────────────────────────

    private static AppUser MakeDoctor(int id, int clinicId = 1) => new AppUser
    {
        Id        = id,
        FirstName = "Doc",
        LastName  = $"#{id}",
        Email     = $"doc{id}@clinic.com",
        Phone     = "0500000000",
        PasswordHash = "x",
        Role      = UserRole.Doctor,
        ClinicId  = clinicId
    };

    private static Patient MakePatient(int id, int clinicId = 1) => new Patient
    {
        Id          = id,
        FirstName   = "Pat",
        LastName    = $"#{id}",
        Phone       = "0500000001",
        ClinicId    = clinicId,
        Gender      = "Male",
        DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static Appointment MakeAppointment(int id, int doctorId, int patientId,
        int clinicId = 1, AppointmentStatus status = AppointmentStatus.Scheduled,
        int offsetHours = 1) => new Appointment
    {
        Id        = id,
        DoctorId  = doctorId,
        PatientId = patientId,
        ClinicId  = clinicId,
        StartTime = DateTime.UtcNow.AddHours(offsetHours),
        EndTime   = DateTime.UtcNow.AddHours(offsetHours + 1),
        Status    = status,
        CreatedAt = DateTime.UtcNow
    };

    private static JsonElement ParseJson(IActionResult result)
    {
        var json = Assert.IsType<JsonResult>(result);
        var raw  = JsonSerializer.Serialize(json.Value);
        return JsonDocument.Parse(raw).RootElement;
    }

    // ── GetCalendarEvents ─────────────────────────────────────────

    [Fact]
    public async Task GetCalendarEvents_ReturnsAllAppointmentsForClinic()
    {
        using var db = TestHelpers.CreateDb(nameof(GetCalendarEvents_ReturnsAllAppointmentsForClinic));

        db.AppUsers.Add(MakeDoctor(10));
        db.Patients.Add(MakePatient(5));
        db.Appointments.AddRange(
            MakeAppointment(1, 10, 5, offsetHours: 1),
            MakeAppointment(2, 10, 5, offsetHours: 3)
        );
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreateAppointmentsController(db, clinicId: 1, role: UserRole.Manager);

        var result = await controller.GetCalendarEvents();
        var root   = ParseJson(result);

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(2, root.GetArrayLength());
    }

    [Fact]
    public async Task GetCalendarEvents_WhenDoctor_ReturnsOnlyOwnAppointments()
    {
        using var db = TestHelpers.CreateDb(nameof(GetCalendarEvents_WhenDoctor_ReturnsOnlyOwnAppointments));

        db.AppUsers.AddRange(MakeDoctor(10), MakeDoctor(20));
        db.Patients.Add(MakePatient(5));
        db.Appointments.AddRange(
            MakeAppointment(1, 10, 5, offsetHours: 1),   // doctor 10
            MakeAppointment(2, 20, 5, offsetHours: 3)    // doctor 20 — should be hidden
        );
        await db.SaveChangesAsync();

        // Logged in as doctor 10
        var controller = TestHelpers.CreateAppointmentsController(db, clinicId: 1, userId: 10, role: UserRole.Doctor);

        var result = await controller.GetCalendarEvents();
        var root   = ParseJson(result);

        Assert.Equal(1, root.GetArrayLength());
        Assert.Equal(1, root[0].GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task GetCalendarEvents_DoesNotReturnOtherClinicAppointments()
    {
        using var db = TestHelpers.CreateDb(nameof(GetCalendarEvents_DoesNotReturnOtherClinicAppointments));

        db.AppUsers.AddRange(MakeDoctor(10, clinicId: 1), MakeDoctor(20, clinicId: 2));
        db.Patients.AddRange(MakePatient(5, clinicId: 1), MakePatient(6, clinicId: 2));
        db.Appointments.AddRange(
            MakeAppointment(1, 10, 5, clinicId: 1),
            MakeAppointment(2, 20, 6, clinicId: 2)   // different clinic
        );
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreateAppointmentsController(db, clinicId: 1, role: UserRole.Manager);

        var result = await controller.GetCalendarEvents();
        var root   = ParseJson(result);

        Assert.Equal(1, root.GetArrayLength());
    }

    // ── GetAppointment ────────────────────────────────────────────

    [Fact]
    public async Task GetAppointment_WhenFound_ReturnsCorrectFields()
    {
        using var db = TestHelpers.CreateDb(nameof(GetAppointment_WhenFound_ReturnsCorrectFields));

        db.AppUsers.Add(MakeDoctor(10));
        db.Patients.Add(MakePatient(5));
        var appt = MakeAppointment(1, 10, 5);
        appt.ReasonForVisit = "Checkup";
        appt.Notes          = "Bring X-ray";
        db.Appointments.Add(appt);
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreateAppointmentsController(db);
        var result     = await controller.GetAppointment(1);
        var root       = ParseJson(result);

        Assert.Equal(1,         root.GetProperty("id").GetInt32());
        Assert.Equal("Checkup", root.GetProperty("reasonForVisit").GetString());
        Assert.Equal("Bring X-ray", root.GetProperty("notes").GetString());
        Assert.Equal("Pat #5",  root.GetProperty("patientName").GetString());
        Assert.Contains("Doc #10", root.GetProperty("doctorName").GetString());
    }

    [Fact]
    public async Task GetAppointment_WhenNotFound_ReturnsNotFound()
    {
        using var db = TestHelpers.CreateDb(nameof(GetAppointment_WhenNotFound_ReturnsNotFound));
        var controller = TestHelpers.CreateAppointmentsController(db);

        var result = await controller.GetAppointment(999);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── GetAvailableDoctors ───────────────────────────────────────

    [Fact]
    public async Task GetAvailableDoctors_WhenDoctorBusy_ExcludesHim()
    {
        using var db = TestHelpers.CreateDb(nameof(GetAvailableDoctors_WhenDoctorBusy_ExcludesHim));

        db.AppUsers.AddRange(MakeDoctor(10), MakeDoctor(20));
        db.Patients.Add(MakePatient(5));

        // Doctor 10 has an appointment from T+1 to T+2
        var busy = MakeAppointment(1, 10, 5, offsetHours: 1);
        db.Appointments.Add(busy);
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreateAppointmentsController(db, clinicId: 1, role: UserRole.Manager);

        // Request window overlaps the busy appointment
        var start = busy.StartTime.ToString("yyyy-MM-ddTHH:mm");
        var end   = busy.EndTime.ToString("yyyy-MM-ddTHH:mm");

        var result = await controller.GetAvailableDoctors(start, end);
        var root   = ParseJson(result);

        var ids = Enumerable.Range(0, root.GetArrayLength())
                            .Select(i => root[i].GetProperty("value").GetInt32())
                            .ToList();

        Assert.DoesNotContain(10, ids);   // busy
        Assert.Contains(20, ids);         // free
    }

    [Fact]
    public async Task GetAvailableDoctors_WhenInvalidTimes_ReturnsEmptyList()
    {
        using var db = TestHelpers.CreateDb(nameof(GetAvailableDoctors_WhenInvalidTimes_ReturnsEmptyList));
        var controller = TestHelpers.CreateAppointmentsController(db);

        var result = await controller.GetAvailableDoctors("not-a-date", "also-not");
        var root   = ParseJson(result);

        Assert.Equal(0, root.GetArrayLength());
    }

    [Fact]
    public async Task GetAvailableDoctors_WhenNoDoctorsInClinic_ReturnsEmptyList()
    {
        using var db = TestHelpers.CreateDb(nameof(GetAvailableDoctors_WhenNoDoctorsInClinic_ReturnsEmptyList));
        var controller = TestHelpers.CreateAppointmentsController(db, clinicId: 1);

        var start = DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm");
        var end   = DateTime.UtcNow.AddHours(2).ToString("yyyy-MM-ddTHH:mm");

        var result = await controller.GetAvailableDoctors(start, end);
        var root   = ParseJson(result);

        Assert.Equal(0, root.GetArrayLength());
    }

    [Fact]
    public async Task GetAvailableDoctors_WhenCancelledConflict_DoctorStillAvailable()
    {
        using var db = TestHelpers.CreateDb(nameof(GetAvailableDoctors_WhenCancelledConflict_DoctorStillAvailable));

        db.AppUsers.Add(MakeDoctor(10));
        db.Patients.Add(MakePatient(5));

        // Cancelled appointment — should NOT block availability
        var cancelled = MakeAppointment(1, 10, 5, offsetHours: 1, status: AppointmentStatus.Cancelled);
        db.Appointments.Add(cancelled);
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreateAppointmentsController(db, clinicId: 1, role: UserRole.Manager);

        var start = cancelled.StartTime.ToString("yyyy-MM-ddTHH:mm");
        var end   = cancelled.EndTime.ToString("yyyy-MM-ddTHH:mm");

        var result = await controller.GetAvailableDoctors(start, end);
        var root   = ParseJson(result);

        Assert.Equal(1, root.GetArrayLength());
        Assert.Equal(10, root[0].GetProperty("value").GetInt32());
    }

    // ── Edit ──────────────────────────────────────────────────────

    [Fact]
    public async Task Edit_WhenValid_UpdatesAppointmentAndRedirectsToCalendar()
    {
        using var db = TestHelpers.CreateDb(nameof(Edit_WhenValid_UpdatesAppointmentAndRedirectsToCalendar));

        db.AppUsers.Add(MakeDoctor(10));
        db.Patients.Add(MakePatient(5));
        db.Appointments.Add(MakeAppointment(1, 10, 5));
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreateAppointmentsController(db, clinicId: 1, role: UserRole.Manager);

        var newStart = DateTime.UtcNow.AddDays(1).AddHours(9);
        var model = new EditAppointmentViewModel
        {
            Id        = 1,
            DoctorId  = 10,
            StartTime = newStart,
            EndTime   = newStart.AddHours(1),
            Status    = AppointmentStatus.Completed,
            ReasonForVisit = "Follow-up",
            Notes          = "Good progress"
        };

        var result = await controller.Edit(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Calendar", redirect.ActionName);

        var updated = await db.Appointments.FindAsync(1);
        Assert.Equal(AppointmentStatus.Completed, updated!.Status);
        Assert.Equal("Follow-up", updated.ReasonForVisit);
    }

    [Fact]
    public async Task Edit_WhenConflictWithOtherAppointment_RedirectsWithError()
    {
        using var db = TestHelpers.CreateDb(nameof(Edit_WhenConflictWithOtherAppointment_RedirectsWithError));

        db.AppUsers.Add(MakeDoctor(10));
        db.Patients.Add(MakePatient(5));
        db.Appointments.AddRange(
            MakeAppointment(1, 10, 5, offsetHours: 1),  // the appointment we edit
            MakeAppointment(2, 10, 5, offsetHours: 3)   // conflicts with new time
        );
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreateAppointmentsController(db, clinicId: 1, role: UserRole.Manager);

        var conflictStart = (await db.Appointments.FindAsync(2))!.StartTime;
        var model = new EditAppointmentViewModel
        {
            Id        = 1,
            DoctorId  = 10,
            StartTime = conflictStart,
            EndTime   = conflictStart.AddHours(1),
            Status    = AppointmentStatus.Scheduled
        };

        var result = await controller.Edit(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Calendar", redirect.ActionName);
        Assert.Equal("Doctor already has an appointment at this time", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Edit_WhenNotFound_RedirectsToCalendarWithError()
    {
        using var db = TestHelpers.CreateDb(nameof(Edit_WhenNotFound_RedirectsToCalendarWithError));
        var controller = TestHelpers.CreateAppointmentsController(db);

        var result = await controller.Edit(new EditAppointmentViewModel
        {
            Id        = 999,
            DoctorId  = 10,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime   = DateTime.UtcNow.AddHours(2),
            Status    = AppointmentStatus.Scheduled
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Calendar", redirect.ActionName);
        Assert.Equal("Appointment not found", controller.TempData["Error"]);
    }

    // ── Delete ────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WhenFound_RemovesAppointmentAndRedirectsToIndex()
    {
        using var db = TestHelpers.CreateDb(nameof(Delete_WhenFound_RemovesAppointmentAndRedirectsToIndex));

        db.AppUsers.Add(MakeDoctor(10));
        db.Patients.Add(MakePatient(5));
        db.Appointments.Add(MakeAppointment(1, 10, 5));
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreateAppointmentsController(db, clinicId: 1, role: UserRole.Manager);

        var result = await controller.Delete(1);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(await db.Appointments.FindAsync(1));
        Assert.Equal("Appointment deleted successfully!", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Delete_WhenNotFound_StillRedirectsWithoutError()
    {
        using var db = TestHelpers.CreateDb(nameof(Delete_WhenNotFound_StillRedirectsWithoutError));
        var controller = TestHelpers.CreateAppointmentsController(db, clinicId: 1, role: UserRole.Manager);

        var result = await controller.Delete(999);

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task Delete_WhenCalendarReferer_RedirectsToCalendar()
    {
        using var db = TestHelpers.CreateDb(nameof(Delete_WhenCalendarReferer_RedirectsToCalendar));

        db.AppUsers.Add(MakeDoctor(10));
        db.Patients.Add(MakePatient(5));
        db.Appointments.Add(MakeAppointment(1, 10, 5));
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreateAppointmentsController(
            db, clinicId: 1, role: UserRole.Manager, referer: "https://app/Appointments/Calendar");

        var result = await controller.Delete(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Calendar", redirect.ActionName);
    }
}
