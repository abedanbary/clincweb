using ClinicApp.Web.Models;
using ClinicApp.Web.Controllers;

using ClinicApp.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicApp.Tests.Controllers;

public class AppointmentsControllerTests
{
    [Fact]
    public async Task GetAppointment_WhenNotFound_ReturnsNotFound()
    {
        using var db = TestHelpers.CreateDb(nameof(GetAppointment_WhenNotFound_ReturnsNotFound));
        var controller = TestHelpers.CreateAppointmentsController(db);

        var result = await controller.GetAppointment(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_WhenConflictExists_RedirectsToIndex_WithError()
    {
        using var db = TestHelpers.CreateDb(nameof(Create_WhenConflictExists_RedirectsToIndex_WithError));

        db.Patients.Add(new Patient
{
    Id = 5,
    FirstName = "Ali",
    LastName = "A",
    Phone = "050",
    ClinicId = 1,
    Gender = "Male",
    DateOfBirth = DateTime.UtcNow.AddYears(-30)
});


        db.Appointments.Add(new Appointment
        {
            Id = 1,
            ClinicId = 1,
            PatientId = 5,
            DoctorId = 10,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Status = AppointmentStatus.Scheduled
        });

        await db.SaveChangesAsync();

        var controller = TestHelpers.CreateAppointmentsController(db, clinicId: 1, userId: 99, role: UserRole.Manager);

        var vm = new AppointmentsPageViewModel
        {
            NewAppointment = new CreateAppointmentViewModel
            {
                IsNewPatient = false,
                ExistingPatientId = 5,
                DoctorId = 10,
                StartTime = DateTime.UtcNow.AddHours(1.5),
                EndTime = DateTime.UtcNow.AddHours(2.5),
                ReasonForVisit = "Check"
            }
        };

        var result = await controller.Create(vm);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Doctor already has an appointment at this time", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Edit_WhenAppointmentNotFound_RedirectsToCalendar_WithError()
    {
        using var db = TestHelpers.CreateDb(nameof(Edit_WhenAppointmentNotFound_RedirectsToCalendar_WithError));
        var controller = TestHelpers.CreateAppointmentsController(db);

        var model = new EditAppointmentViewModel
        {
            Id = 123,
            DoctorId = 10,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Status = AppointmentStatus.Scheduled
        };

        var result = await controller.Edit(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Calendar", redirect.ActionName);
        Assert.Equal("Appointment not found", controller.TempData["Error"]);

    }


    [Fact]
    public async Task Create_WhenInPast_RedirectsWithError()
    {
        using var db = TestHelpers.CreateDb(nameof(Create_WhenInPast_RedirectsWithError));

        db.Patients.Add(new Patient
        {
            Id = 5, FirstName = "Ali", LastName = "A", Phone = "050",
            ClinicId = 1, Gender = "Male", DateOfBirth = DateTime.UtcNow.AddYears(-30)
        });
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreateAppointmentsController(db, clinicId: 1, userId: 99, role: UserRole.Manager);

        var vm = new AppointmentsPageViewModel
        {
            NewAppointment = new CreateAppointmentViewModel
            {
                IsNewPatient = false,
                ExistingPatientId = 5,
                DoctorId = 10,
                StartTime = DateTime.UtcNow.AddHours(-2),   // past
                EndTime   = DateTime.UtcNow.AddHours(-1),
                ReasonForVisit = "Check"
            }
        };

        var result = await controller.Create(vm);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Cannot create appointments in the past", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Create_WhenPatientConflict_RedirectsWithError()
    {
        using var db = TestHelpers.CreateDb(nameof(Create_WhenPatientConflict_RedirectsWithError));

        db.Patients.Add(new Patient
        {
            Id = 5, FirstName = "Ali", LastName = "A", Phone = "050",
            ClinicId = 1, Gender = "Male", DateOfBirth = DateTime.UtcNow.AddYears(-30)
        });
        // Patient already booked with doctor 20 in the same slot
        db.Appointments.Add(new Appointment
        {
            Id = 1, ClinicId = 1, PatientId = 5, DoctorId = 20,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime   = DateTime.UtcNow.AddHours(2),
            Status    = AppointmentStatus.Scheduled
        });
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreateAppointmentsController(db, clinicId: 1, userId: 99, role: UserRole.Manager);

        var vm = new AppointmentsPageViewModel
        {
            NewAppointment = new CreateAppointmentViewModel
            {
                IsNewPatient = false,
                ExistingPatientId = 5,
                DoctorId = 10,                              // different doctor — no doctor conflict
                StartTime = DateTime.UtcNow.AddHours(1.5),
                EndTime   = DateTime.UtcNow.AddHours(2.5),
                ReasonForVisit = "Check"
            }
        };

        var result = await controller.Create(vm);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("This patient already has an appointment at this time", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Create_WhenDoctorRole_DoctorIdIsOverriddenToSelf()
    {
        using var db = TestHelpers.CreateDb(nameof(Create_WhenDoctorRole_DoctorIdIsOverriddenToSelf));

        db.Patients.Add(new Patient
        {
            Id = 5, FirstName = "Ali", LastName = "A", Phone = "050",
            ClinicId = 1, Gender = "Male", DateOfBirth = DateTime.UtcNow.AddYears(-30)
        });
        await db.SaveChangesAsync();

        // Logged in as doctor 10; the form sends DoctorId = 20 (another doctor)
        var controller = TestHelpers.CreateAppointmentsController(db, clinicId: 1, userId: 10, role: UserRole.Doctor);

        var start = DateTime.UtcNow.AddDays(1).AddHours(1);
        var vm = new AppointmentsPageViewModel
        {
            NewAppointment = new CreateAppointmentViewModel
            {
                IsNewPatient = false,
                ExistingPatientId = 5,
                DoctorId = 20,   // attacker tries to book for another doctor
                StartTime = start,
                EndTime   = start.AddHours(1),
                ReasonForVisit = "Self-booking test"
            }
        };

        await controller.Create(vm);

        var created = await db.Appointments.FirstOrDefaultAsync();
        Assert.NotNull(created);
        Assert.Equal(10, created!.DoctorId);   // must be doctor's own ID, not 20
    }

    [Fact]
    public async Task Create_WhenCancelledConflictExists_StillAllowsBooking()
    {
        using var db = TestHelpers.CreateDb(nameof(Create_WhenCancelledConflictExists_StillAllowsBooking));

        db.Patients.Add(new Patient
        {
            Id = 5, FirstName = "Ali", LastName = "A", Phone = "050",
            ClinicId = 1, Gender = "Male", DateOfBirth = DateTime.UtcNow.AddYears(-30)
        });
        // Cancelled appointment — should NOT block a new one
        db.Appointments.Add(new Appointment
        {
            Id = 1, ClinicId = 1, PatientId = 5, DoctorId = 10,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime   = DateTime.UtcNow.AddHours(2),
            Status    = AppointmentStatus.Cancelled
        });
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreateAppointmentsController(db, clinicId: 1, userId: 99, role: UserRole.Manager);

        var start = DateTime.UtcNow.AddHours(1);
        var vm = new AppointmentsPageViewModel
        {
            NewAppointment = new CreateAppointmentViewModel
            {
                IsNewPatient = false,
                ExistingPatientId = 5,
                DoctorId = 10,
                StartTime = start,
                EndTime   = start.AddHours(1),
                ReasonForVisit = "Post-cancel rebooking"
            }
        };

        await controller.Create(vm);

        Assert.Equal("Appointment created successfully!", controller.TempData["Success"]);
    }

    [Fact]
public async Task Create_WhenValid_AddsAppointment_AndRedirectsToIndex()
{
    using var db = TestHelpers.CreateDb(nameof(Create_WhenValid_AddsAppointment_AndRedirectsToIndex));

    // Seed Patient required fields
    db.Patients.Add(new Patient
    {
        Id = 5,
        FirstName = "Ali",
        LastName = "A",
        Phone = "050",
        ClinicId = 1,
        Gender = "Male",
        DateOfBirth = DateTime.UtcNow.AddYears(-30)
    });
    await db.SaveChangesAsync();

    var controller = TestHelpers.CreateAppointmentsController(db, clinicId: 1, userId: 99, role: UserRole.Manager);

    var start = DateTime.UtcNow.AddDays(1).AddHours(1);
    var end = start.AddHours(1);

    var vm = new AppointmentsPageViewModel
    {
        NewAppointment = new CreateAppointmentViewModel
        {
            IsNewPatient = false,
            ExistingPatientId = 5,
            DoctorId = 10,
            StartTime = start,
            EndTime = end,
            ReasonForVisit = "Check",
            Notes = "Unit test"
        }
    };

    var result = await controller.Create(vm);

    // Assert redirect
    var redirect = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal("Index", redirect.ActionName);

    // Assert DB has new appointment
    var created = await db.Appointments.OrderByDescending(a => a.Id).FirstOrDefaultAsync();
    Assert.NotNull(created);
    Assert.Equal(1, created!.ClinicId);
    Assert.Equal(5, created.PatientId);
    Assert.Equal(10, created.DoctorId);
    Assert.Equal(AppointmentStatus.Scheduled, created.Status);

    // Optional: check success message
    Assert.Equal("Appointment created successfully!", controller.TempData["Success"]);
}

}
