using ClinicApp.Web.Models;
using ClinicApp.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicApp.Tests.Controllers;

public class PaymentsControllerTests
{
    // ── Helpers ────────────────────────────────────────────────

    private static Patient SeedPatient(int id = 5, int clinicId = 1) => new Patient
    {
        Id = id,
        FirstName = "John",
        LastName = "Doe",
        Phone = "050-000-0000",
        ClinicId = clinicId,
        Gender = "Male",
        DateOfBirth = DateTime.UtcNow.AddYears(-30)
    };

    private static AppUser SeedDoctor(int id = 10, int clinicId = 1) => new AppUser
    {
        Id = id,
        FirstName = "Alice",
        LastName = "Smith",
        Email = $"alice{id}@clinic.com",
        Phone = "050-000-0000",
        ClinicId = clinicId,
        Role = UserRole.Doctor,
        PasswordHash = "hash"
    };

    private static Payment SeedPayment(int id = 1, int clinicId = 1, int patientId = 5, int doctorId = 10) => new Payment
    {
        Id = id,
        InvoiceNumber = 1001,
        PatientId = patientId,
        DoctorId = doctorId,
        Amount = 250m,
        Method = PaymentMethod.Cash,
        Status = PaymentStatus.Paid,
        PaymentDate = DateTime.UtcNow,
        ClinicId = clinicId
    };

    // ── Index Tests ─────────────────────────────────────────────

    [Fact]
    public async Task Index_Manager_ReturnsViewWithAllClinicPayments()
    {
        using var db = TestHelpers.CreateDb(nameof(Index_Manager_ReturnsViewWithAllClinicPayments));

        db.Patients.Add(SeedPatient());
        db.AppUsers.Add(SeedDoctor());
        db.Payments.Add(SeedPayment());
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreatePaymentsController(db, clinicId: 1, userId: 99, role: UserRole.Manager);

        var result = await controller.Index(null, null, null, null);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<PaymentsPageViewModel>(view.Model);
        Assert.Single(vm.Payments);
    }

    [Fact]
    public async Task Index_Doctor_SeesOnlyOwnPatientPayments()
    {
        using var db = TestHelpers.CreateDb(nameof(Index_Doctor_SeesOnlyOwnPatientPayments));

        db.Patients.Add(SeedPatient(5));
        db.Patients.Add(SeedPatient(6, 1));
        db.AppUsers.Add(SeedDoctor(10));
        db.AppUsers.Add(SeedDoctor(20, 1));
        db.Payments.Add(SeedPayment(1, 1, patientId: 5, doctorId: 10));
        db.Payments.Add(SeedPayment(2, 1, patientId: 6, doctorId: 20));
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreatePaymentsController(db, clinicId: 1, userId: 10, role: UserRole.Doctor);

        var result = await controller.Index(null, null, null, null);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<PaymentsPageViewModel>(view.Model);
        Assert.Single(vm.Payments);
        Assert.Equal(10, vm.Payments[0].DoctorId);
    }

    [Fact]
    public async Task Index_FilterByPatient_ReturnsFilteredPayments()
    {
        using var db = TestHelpers.CreateDb(nameof(Index_FilterByPatient_ReturnsFilteredPayments));

        db.Patients.Add(SeedPatient(5));
        db.Patients.Add(SeedPatient(6, 1));
        db.AppUsers.Add(SeedDoctor(10));
        db.Payments.Add(SeedPayment(1, 1, patientId: 5, doctorId: 10));
        db.Payments.Add(SeedPayment(2, 1, patientId: 6, doctorId: 10));
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreatePaymentsController(db, clinicId: 1, userId: 99, role: UserRole.Manager);

        var result = await controller.Index(patientId: 5, null, null, null);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<PaymentsPageViewModel>(view.Model);
        Assert.Single(vm.Payments);
        Assert.Equal(5, vm.Payments[0].PatientId);
    }

    [Fact]
    public async Task Index_Stats_CalculatedCorrectly()
    {
        using var db = TestHelpers.CreateDb(nameof(Index_Stats_CalculatedCorrectly));

        db.Patients.Add(SeedPatient());
        db.AppUsers.Add(SeedDoctor());
        db.Payments.Add(new Payment { Id = 1, InvoiceNumber = 1001, PatientId = 5, DoctorId = 10, Amount = 200m, Method = PaymentMethod.Cash, Status = PaymentStatus.Paid, PaymentDate = DateTime.UtcNow, ClinicId = 1 });
        db.Payments.Add(new Payment { Id = 2, InvoiceNumber = 1002, PatientId = 5, DoctorId = 10, Amount = 100m, Method = PaymentMethod.Cash, Status = PaymentStatus.Pending, PaymentDate = DateTime.UtcNow, ClinicId = 1 });
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreatePaymentsController(db, clinicId: 1, role: UserRole.Manager);
        var result = await controller.Index(null, null, null, null);

        var vm = Assert.IsType<PaymentsPageViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(200m, vm.TotalRevenue);
        Assert.Equal(100m, vm.PendingAmount);
        Assert.Equal(1, vm.PaidCount);
        Assert.Equal(1, vm.PendingCount);
    }

    // ── Create Tests ────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidPayment_SavesAndRedirects()
    {
        using var db = TestHelpers.CreateDb(nameof(Create_ValidPayment_SavesAndRedirects));

        db.Patients.Add(SeedPatient());
        db.AppUsers.Add(SeedDoctor());
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreatePaymentsController(db, clinicId: 1, userId: 99, role: UserRole.Manager);

        var vm = new PaymentsPageViewModel
        {
            NewPayment = new CreatePaymentViewModel
            {
                PatientId = 5,
                DoctorId = 10,
                Amount = 350m,
                Method = PaymentMethod.CreditCard,
                Status = PaymentStatus.Paid,
                PaymentDate = DateTime.Today
            }
        };

        var result = await controller.Create(vm);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var saved = await db.Payments.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal(350m, saved!.Amount);
        Assert.Equal(PaymentMethod.CreditCard, saved.Method);
        Assert.Equal(1, saved.ClinicId);
        Assert.Contains("created successfully", controller.TempData["Success"]?.ToString());
    }

    [Fact]
    public async Task Create_InvalidAmount_RedirectsWithError()
    {
        using var db = TestHelpers.CreateDb(nameof(Create_InvalidAmount_RedirectsWithError));

        var controller = TestHelpers.CreatePaymentsController(db, clinicId: 1, role: UserRole.Manager);

        var vm = new PaymentsPageViewModel
        {
            NewPayment = new CreatePaymentViewModel
            {
                PatientId = 5,
                DoctorId = 10,
                Amount = 0,
                Method = PaymentMethod.Cash,
                Status = PaymentStatus.Paid,
                PaymentDate = DateTime.Today
            }
        };

        var result = await controller.Create(vm);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.NotNull(controller.TempData["Error"]);
    }

    [Fact]
    public async Task Create_InvoiceNumberAutoIncrements()
    {
        using var db = TestHelpers.CreateDb(nameof(Create_InvoiceNumberAutoIncrements));

        db.Patients.Add(SeedPatient());
        db.AppUsers.Add(SeedDoctor());
        db.Payments.Add(SeedPayment(1, 1, 5, 10));
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreatePaymentsController(db, clinicId: 1, role: UserRole.Manager);

        var vm = new PaymentsPageViewModel
        {
            NewPayment = new CreatePaymentViewModel
            {
                PatientId = 5, DoctorId = 10,
                Amount = 100m, Method = PaymentMethod.Cash,
                Status = PaymentStatus.Paid, PaymentDate = DateTime.Today
            }
        };

        await controller.Create(vm);

        var newPayment = await db.Payments.OrderByDescending(p => p.Id).FirstAsync();
        Assert.Equal(1002, newPayment.InvoiceNumber);
    }

    // ── Edit Tests ──────────────────────────────────────────────

    [Fact]
    public async Task Edit_ExistingPayment_UpdatesAndRedirects()
    {
        using var db = TestHelpers.CreateDb(nameof(Edit_ExistingPayment_UpdatesAndRedirects));

        db.Patients.Add(SeedPatient());
        db.AppUsers.Add(SeedDoctor());
        db.Payments.Add(SeedPayment());
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreatePaymentsController(db, clinicId: 1, role: UserRole.Manager);

        var model = new EditPaymentViewModel
        {
            Id = 1,
            PatientId = 5,
            DoctorId = 10,
            Amount = 500m,
            Method = PaymentMethod.BankTransfer,
            Status = PaymentStatus.Pending,
            PaymentDate = DateTime.Today
        };

        var result = await controller.Edit(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var updated = await db.Payments.FindAsync(1);
        Assert.Equal(500m, updated!.Amount);
        Assert.Equal(PaymentStatus.Pending, updated.Status);
        Assert.Equal(PaymentMethod.BankTransfer, updated.Method);
    }

    [Fact]
    public async Task Edit_PaymentNotFound_RedirectsWithError()
    {
        using var db = TestHelpers.CreateDb(nameof(Edit_PaymentNotFound_RedirectsWithError));

        var controller = TestHelpers.CreatePaymentsController(db, clinicId: 1, role: UserRole.Manager);

        var model = new EditPaymentViewModel
        {
            Id = 999,
            PatientId = 5, DoctorId = 10,
            Amount = 100m, Method = PaymentMethod.Cash,
            Status = PaymentStatus.Paid, PaymentDate = DateTime.Today
        };

        var result = await controller.Edit(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Payment not found.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Edit_CannotEditPaymentFromOtherClinic()
    {
        using var db = TestHelpers.CreateDb(nameof(Edit_CannotEditPaymentFromOtherClinic));

        db.Patients.Add(SeedPatient(5, 2));
        db.AppUsers.Add(SeedDoctor(10, 2));
        db.Payments.Add(SeedPayment(1, clinicId: 2));
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreatePaymentsController(db, clinicId: 1, role: UserRole.Manager);

        var model = new EditPaymentViewModel
        {
            Id = 1, PatientId = 5, DoctorId = 10,
            Amount = 999m, Method = PaymentMethod.Cash,
            Status = PaymentStatus.Paid, PaymentDate = DateTime.Today
        };

        var result = await controller.Edit(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Payment not found.", controller.TempData["Error"]);

        var unchanged = await db.Payments.FindAsync(1);
        Assert.Equal(250m, unchanged!.Amount);
    }

    // ── Delete Tests ─────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingPayment_RemovesAndRedirects()
    {
        using var db = TestHelpers.CreateDb(nameof(Delete_ExistingPayment_RemovesAndRedirects));

        db.Patients.Add(SeedPatient());
        db.AppUsers.Add(SeedDoctor());
        db.Payments.Add(SeedPayment());
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreatePaymentsController(db, clinicId: 1, role: UserRole.Manager);

        var result = await controller.Delete(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(0, await db.Payments.CountAsync());
        Assert.Contains("deleted successfully", controller.TempData["Success"]?.ToString());
    }

    [Fact]
    public async Task Delete_PaymentNotFound_RedirectsWithError()
    {
        using var db = TestHelpers.CreateDb(nameof(Delete_PaymentNotFound_RedirectsWithError));

        var controller = TestHelpers.CreatePaymentsController(db, clinicId: 1, role: UserRole.Manager);

        var result = await controller.Delete(999);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Payment not found.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Delete_CannotDeletePaymentFromOtherClinic()
    {
        using var db = TestHelpers.CreateDb(nameof(Delete_CannotDeletePaymentFromOtherClinic));

        db.Patients.Add(SeedPatient(5, 2));
        db.AppUsers.Add(SeedDoctor(10, 2));
        db.Payments.Add(SeedPayment(1, clinicId: 2));
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreatePaymentsController(db, clinicId: 1, role: UserRole.Manager);

        await controller.Delete(1);

        Assert.Equal(1, await db.Payments.CountAsync());
    }

    // ── GetPayment API Tests ─────────────────────────────────────

    [Fact]
    public async Task GetPayment_ExistingId_ReturnsJson()
    {
        using var db = TestHelpers.CreateDb(nameof(GetPayment_ExistingId_ReturnsJson));

        db.Patients.Add(SeedPatient());
        db.AppUsers.Add(SeedDoctor());
        db.Payments.Add(SeedPayment());
        await db.SaveChangesAsync();

        var controller = TestHelpers.CreatePaymentsController(db, clinicId: 1, role: UserRole.Manager);

        var result = await controller.GetPayment(1);

        Assert.IsType<JsonResult>(result);
    }

    [Fact]
    public async Task GetPayment_NotFound_ReturnsNotFound()
    {
        using var db = TestHelpers.CreateDb(nameof(GetPayment_NotFound_ReturnsNotFound));

        var controller = TestHelpers.CreatePaymentsController(db, clinicId: 1, role: UserRole.Manager);

        var result = await controller.GetPayment(999);

        Assert.IsType<NotFoundResult>(result);
    }
}
