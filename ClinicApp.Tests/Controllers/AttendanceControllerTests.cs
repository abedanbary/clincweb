using ClinicApp.Web.Models;
using ClinicApp.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ClinicApp.Tests.Controllers;

public class AttendanceControllerTests
{
    // ── Seed helpers ────────────────────────────────────────────

    private static AppUser SeedDoctor(int id = 10, int clinicId = 1) => new AppUser
    {
        Id = id, FirstName = "Alice", LastName = "Smith",
        Email = $"alice{id}@clinic.com", Phone = "050-000-0000",
        ClinicId = clinicId, Role = UserRole.Doctor, PasswordHash = "hash"
    };

    private static DoctorAttendance SeedRecord(
        int id = 1, int doctorId = 10, int clinicId = 1,
        AttendanceStatus status = AttendanceStatus.Present,
        DateTime? date = null) => new DoctorAttendance
    {
        Id = id, DoctorId = doctorId, ClinicId = clinicId,
        Date = date ?? DateTime.UtcNow.Date,
        Status = status
    };

    // ── Index (Manager) ─────────────────────────────────────────

    [Fact]
    public async Task Index_Manager_ReturnsView()
    {
        using var db = TestHelpers.CreateDb(nameof(Index_Manager_ReturnsView));
        db.AppUsers.Add(SeedDoctor());
        await db.SaveChangesAsync();

        var ctrl = TestHelpers.CreateAttendanceController(db, role: UserRole.Manager);
        var result = await ctrl.Index(null, null, null);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<AttendancePageViewModel>(view.Model);
        Assert.Single(vm.Doctors);
    }

    [Fact]
    public async Task Index_Doctor_RedirectsToMyAttendance()
    {
        using var db = TestHelpers.CreateDb(nameof(Index_Doctor_RedirectsToMyAttendance));
        db.AppUsers.Add(SeedDoctor());
        await db.SaveChangesAsync();

        var ctrl = TestHelpers.CreateAttendanceController(db, role: UserRole.Doctor);
        var result = await ctrl.Index(null, null, null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("MyAttendance", redirect.ActionName);
    }

    [Fact]
    public async Task Index_Manager_FiltersByDoctorAndMonth()
    {
        using var db = TestHelpers.CreateDb(nameof(Index_Manager_FiltersByDoctorAndMonth));
        db.AppUsers.Add(SeedDoctor(10));
        db.DoctorAttendances.Add(SeedRecord(date: new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var ctrl = TestHelpers.CreateAttendanceController(db, role: UserRole.Manager);
        var result = await ctrl.Index(10, 2025, 6);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<AttendancePageViewModel>(view.Model);
        Assert.Single(vm.Records);
        Assert.Equal(2025, vm.Year);
        Assert.Equal(6, vm.Month);
    }

    // ── MyAttendance (Doctor) ───────────────────────────────────

    [Fact]
    public async Task MyAttendance_Doctor_ReturnsOwnRecords()
    {
        using var db = TestHelpers.CreateDb(nameof(MyAttendance_Doctor_ReturnsOwnRecords));
        db.AppUsers.Add(SeedDoctor(10));
        var now = DateTime.UtcNow;
        db.DoctorAttendances.Add(SeedRecord(doctorId: 10, date: new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var ctrl = TestHelpers.CreateAttendanceController(db, userId: 10, role: UserRole.Doctor);
        var result = await ctrl.MyAttendance(null, null);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<AttendancePageViewModel>(view.Model);
        Assert.Single(vm.Records);
        Assert.Equal(10, vm.SelectedDoctorId);
    }

    [Fact]
    public async Task MyAttendance_OtherClinicRecord_NotReturned()
    {
        using var db = TestHelpers.CreateDb(nameof(MyAttendance_OtherClinicRecord_NotReturned));
        db.AppUsers.Add(SeedDoctor(10, clinicId: 1));
        var now = DateTime.UtcNow;
        db.DoctorAttendances.Add(SeedRecord(doctorId: 10, clinicId: 2,
            date: new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var ctrl = TestHelpers.CreateAttendanceController(db, clinicId: 1, userId: 10, role: UserRole.Doctor);
        var result = await ctrl.MyAttendance(null, null);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<AttendancePageViewModel>(view.Model);
        Assert.Empty(vm.Records);
    }

    // ── Save ────────────────────────────────────────────────────

    [Fact]
    public async Task Save_Manager_CreatesNewRecord()
    {
        using var db = TestHelpers.CreateDb(nameof(Save_Manager_CreatesNewRecord));
        db.AppUsers.Add(SeedDoctor(10));
        await db.SaveChangesAsync();

        var ctrl = TestHelpers.CreateAttendanceController(db, role: UserRole.Manager);
        var model = new SaveAttendanceViewModel
        {
            DoctorId = 10,
            Date = new DateTime(2025, 6, 5, 0, 0, 0, DateTimeKind.Utc),
            Status = AttendanceStatus.Present,
            CheckIn = "09:00", CheckOut = "17:00"
        };

        var result = await ctrl.Save(model);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(1, db.DoctorAttendances.Count());
    }

    [Fact]
    public async Task Save_Manager_UpdatesExistingRecord()
    {
        using var db = TestHelpers.CreateDb(nameof(Save_Manager_UpdatesExistingRecord));
        db.AppUsers.Add(SeedDoctor(10));
        db.DoctorAttendances.Add(SeedRecord(date: new DateTime(2025, 6, 5, 0, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var ctrl = TestHelpers.CreateAttendanceController(db, role: UserRole.Manager);
        var model = new SaveAttendanceViewModel
        {
            DoctorId = 10,
            Date = new DateTime(2025, 6, 5, 0, 0, 0, DateTimeKind.Utc),
            Status = AttendanceStatus.Absent
        };

        await ctrl.Save(model);

        var rec = db.DoctorAttendances.Single();
        Assert.Equal(AttendanceStatus.Absent, rec.Status);
    }

    [Fact]
    public async Task Save_Present_StoresCheckInOut()
    {
        using var db = TestHelpers.CreateDb(nameof(Save_Present_StoresCheckInOut));
        db.AppUsers.Add(SeedDoctor(10));
        await db.SaveChangesAsync();

        var ctrl = TestHelpers.CreateAttendanceController(db, role: UserRole.Manager);
        await ctrl.Save(new SaveAttendanceViewModel
        {
            DoctorId = 10,
            Date = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = AttendanceStatus.Present,
            CheckIn = "08:30", CheckOut = "16:30"
        });

        var rec = db.DoctorAttendances.Single();
        Assert.Equal(TimeSpan.Parse("08:30"), rec.CheckIn);
        Assert.Equal(TimeSpan.Parse("16:30"), rec.CheckOut);
        Assert.Equal(8.0, rec.WorkingHours);
    }

    [Fact]
    public async Task Save_Absent_ClearsCheckInOut()
    {
        using var db = TestHelpers.CreateDb(nameof(Save_Absent_ClearsCheckInOut));
        db.AppUsers.Add(SeedDoctor(10));
        db.DoctorAttendances.Add(new DoctorAttendance
        {
            DoctorId = 10, ClinicId = 1,
            Date = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = AttendanceStatus.Present,
            CheckIn = TimeSpan.Parse("09:00"), CheckOut = TimeSpan.Parse("17:00")
        });
        await db.SaveChangesAsync();

        var ctrl = TestHelpers.CreateAttendanceController(db, role: UserRole.Manager);
        await ctrl.Save(new SaveAttendanceViewModel
        {
            DoctorId = 10,
            Date = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = AttendanceStatus.Absent
        });

        var rec = db.DoctorAttendances.Single();
        Assert.Null(rec.CheckIn);
        Assert.Null(rec.CheckOut);
    }

    [Fact]
    public async Task Save_Doctor_CannotSaveOtherDoctorRecord()
    {
        using var db = TestHelpers.CreateDb(nameof(Save_Doctor_CannotSaveOtherDoctorRecord));
        db.AppUsers.Add(SeedDoctor(10));
        db.AppUsers.Add(SeedDoctor(20));
        await db.SaveChangesAsync();

        var ctrl = TestHelpers.CreateAttendanceController(db, userId: 10, role: UserRole.Doctor);
        var result = await ctrl.Save(new SaveAttendanceViewModel
        {
            DoctorId = 20,
            Date = DateTime.UtcNow.Date,
            Status = AttendanceStatus.Present
        });

        Assert.IsType<ForbidResult>(result);
        Assert.Equal(0, db.DoctorAttendances.Count());
    }

    [Fact]
    public async Task Save_Doctor_RedirectsToMyAttendance()
    {
        using var db = TestHelpers.CreateDb(nameof(Save_Doctor_RedirectsToMyAttendance));
        db.AppUsers.Add(SeedDoctor(10));
        await db.SaveChangesAsync();

        var ctrl = TestHelpers.CreateAttendanceController(db, userId: 10, role: UserRole.Doctor);
        var result = await ctrl.Save(new SaveAttendanceViewModel
        {
            DoctorId = 10,
            Date = new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = AttendanceStatus.Present
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("MyAttendance", redirect.ActionName);
    }

    [Fact]
    public async Task Save_Manager_RedirectsToIndex()
    {
        using var db = TestHelpers.CreateDb(nameof(Save_Manager_RedirectsToIndex));
        db.AppUsers.Add(SeedDoctor(10));
        await db.SaveChangesAsync();

        var ctrl = TestHelpers.CreateAttendanceController(db, role: UserRole.Manager);
        var result = await ctrl.Save(new SaveAttendanceViewModel
        {
            DoctorId = 10,
            Date = new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = AttendanceStatus.DayOff
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    // ── Delete ──────────────────────────────────────────────────

    [Fact]
    public async Task Delete_Manager_RemovesRecord()
    {
        using var db = TestHelpers.CreateDb(nameof(Delete_Manager_RemovesRecord));
        db.AppUsers.Add(SeedDoctor(10));
        db.DoctorAttendances.Add(SeedRecord(id: 1));
        await db.SaveChangesAsync();

        var ctrl = TestHelpers.CreateAttendanceController(db, role: UserRole.Manager);
        await ctrl.Delete(1, 10, 2025, 6);

        Assert.Equal(0, db.DoctorAttendances.Count());
    }

    [Fact]
    public async Task Delete_OtherClinicRecord_NotDeleted()
    {
        using var db = TestHelpers.CreateDb(nameof(Delete_OtherClinicRecord_NotDeleted));
        db.AppUsers.Add(SeedDoctor(10, clinicId: 2));
        db.DoctorAttendances.Add(SeedRecord(id: 1, clinicId: 2));
        await db.SaveChangesAsync();

        var ctrl = TestHelpers.CreateAttendanceController(db, clinicId: 1, role: UserRole.Manager);
        await ctrl.Delete(1, 10, 2025, 6);

        Assert.Equal(1, db.DoctorAttendances.Count());
    }

    // ── Summary computations ────────────────────────────────────

    [Fact]
    public async Task Index_Summary_CountsStatuses()
    {
        using var db = TestHelpers.CreateDb(nameof(Index_Summary_CountsStatuses));
        db.AppUsers.Add(SeedDoctor(10));
        db.DoctorAttendances.AddRange(
            SeedRecord(1, date: new DateTime(2025, 6, 2, 0, 0, 0, DateTimeKind.Utc), status: AttendanceStatus.Present),
            SeedRecord(2, date: new DateTime(2025, 6, 3, 0, 0, 0, DateTimeKind.Utc), status: AttendanceStatus.Absent),
            SeedRecord(3, date: new DateTime(2025, 6, 4, 0, 0, 0, DateTimeKind.Utc), status: AttendanceStatus.DayOff)
        );
        await db.SaveChangesAsync();

        var ctrl = TestHelpers.CreateAttendanceController(db, role: UserRole.Manager);
        var result = await ctrl.Index(10, 2025, 6);

        var vm = Assert.IsType<AttendancePageViewModel>(((ViewResult)result).Model);
        Assert.Equal(1, vm.Summary.DaysPresent);
        Assert.Equal(1, vm.Summary.DaysAbsent);
        Assert.Equal(1, vm.Summary.DayOff);
    }

    [Fact]
    public async Task Index_Summary_TotalHours_ComputedCorrectly()
    {
        using var db = TestHelpers.CreateDb(nameof(Index_Summary_TotalHours_ComputedCorrectly));
        db.AppUsers.Add(SeedDoctor(10));
        db.DoctorAttendances.Add(new DoctorAttendance
        {
            Id = 1, DoctorId = 10, ClinicId = 1,
            Date = new DateTime(2025, 6, 2, 0, 0, 0, DateTimeKind.Utc),
            Status = AttendanceStatus.Present,
            CheckIn = TimeSpan.FromHours(9),
            CheckOut = TimeSpan.FromHours(17)
        });
        await db.SaveChangesAsync();

        var ctrl = TestHelpers.CreateAttendanceController(db, role: UserRole.Manager);
        var result = await ctrl.Index(10, 2025, 6);

        var vm = Assert.IsType<AttendancePageViewModel>(((ViewResult)result).Model);
        Assert.Equal(8.0, vm.Summary.TotalHours);
    }

    // ── Calendar grid ───────────────────────────────────────────

    [Fact]
    public async Task Index_CalendarGrid_StartsOnMonday()
    {
        using var db = TestHelpers.CreateDb(nameof(Index_CalendarGrid_StartsOnMonday));
        db.AppUsers.Add(SeedDoctor(10));
        await db.SaveChangesAsync();

        var ctrl = TestHelpers.CreateAttendanceController(db, role: UserRole.Manager);
        var result = await ctrl.Index(10, 2025, 6);

        var vm = Assert.IsType<AttendancePageViewModel>(((ViewResult)result).Model);
        Assert.Equal(DayOfWeek.Monday, vm.CalendarDays.First().Date.DayOfWeek);
    }

    [Fact]
    public async Task Index_CalendarGrid_CountIsMultipleOf7()
    {
        using var db = TestHelpers.CreateDb(nameof(Index_CalendarGrid_CountIsMultipleOf7));
        db.AppUsers.Add(SeedDoctor(10));
        await db.SaveChangesAsync();

        var ctrl = TestHelpers.CreateAttendanceController(db, role: UserRole.Manager);
        var result = await ctrl.Index(10, 2025, 6);

        var vm = Assert.IsType<AttendancePageViewModel>(((ViewResult)result).Model);
        Assert.Equal(0, vm.CalendarDays.Count % 7);
    }
}
