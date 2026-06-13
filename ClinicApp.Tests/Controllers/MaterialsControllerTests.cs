using ClinicApp.Web.Controllers;
using ClinicApp.Web.Models;
using ClinicApp.Web.Services;
using ClinicApp.Web.Services.Storage;
using ClinicApp.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace ClinicApp.Tests.Controllers;

public class MaterialsControllerTests
{
    // ── Factories ─────────────────────────────────────────────────────────────

    private static Web.Data.ApplicationDbContext CreateDb(string name)
    {
        var opts = new DbContextOptionsBuilder<Web.Data.ApplicationDbContext>()
            .UseInMemoryDatabase(name).Options;
        return new Web.Data.ApplicationDbContext(opts);
    }

    private static MaterialsController CreateController(
        Web.Data.ApplicationDbContext db,
        Mock<IFileStorageService>? storageMock = null,
        int clinicId = 1,
        string role = "Manager")
    {
        var exportMock = new Mock<IExportService>();
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("ClinicId", clinicId.ToString()),
            new Claim(ClaimTypes.Role, role)
        }, "TestAuth"));

        var ctrl = new MaterialsController(db, exportMock.Object, storageMock?.Object);
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        return ctrl;
    }

    private static Material SeedMaterial(Web.Data.ApplicationDbContext db,
        int id = 1, int clinicId = 1, string name = "Gloves",
        int qty = 100, string unit = "Box")
    {
        var m = new Material
        {
            Id = id, ClinicId = clinicId, Name = name,
            Quantity = qty, Unit = unit, MinimumLimit = 10
        };
        db.Materials.Add(m);
        db.SaveChanges();
        return m;
    }

    // ── Index ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Index_ReturnsViewWithClinicMaterials()
    {
        using var db = CreateDb(nameof(Index_ReturnsViewWithClinicMaterials));
        SeedMaterial(db, clinicId: 1);
        SeedMaterial(db, id: 2, clinicId: 99, name: "Other Clinic");

        var result = await CreateController(db).Index() as ViewResult;
        var vm = Assert.IsType<MaterialsPageViewModel>(result!.Model);
        Assert.Single(vm.Materials);
        Assert.Equal("Gloves", vm.Materials[0].Name);
    }

    // ── Add ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_NewMaterial_SavesMaterialAndHistory()
    {
        using var db = CreateDb(nameof(Add_NewMaterial_SavesMaterialAndHistory));
        var ctrl = CreateController(db);

        var vm = new MaterialsPageViewModel
        {
            NewMaterial = new Material { Name = "Masks", Unit = "Box", Quantity = 50, MinimumLimit = 5 }
        };

        var result = await ctrl.Add(vm, null) as RedirectToActionResult;

        Assert.Equal("Index", result!.ActionName);
        Assert.Single(db.Materials);
        Assert.Equal(50, db.Materials.First().Quantity);
        Assert.Single(db.MaterialHistories);
        Assert.Equal(50, db.MaterialHistories.First().QuantityChange);
        Assert.Equal("First stock added", db.MaterialHistories.First().Note);
    }

    [Fact]
    public async Task Add_ExistingMaterial_IncreasesQuantityAndAddsHistory()
    {
        using var db = CreateDb(nameof(Add_ExistingMaterial_IncreasesQuantityAndAddsHistory));
        SeedMaterial(db, qty: 100);
        var ctrl = CreateController(db);

        var vm = new MaterialsPageViewModel
        {
            NewMaterial = new Material { Name = "Gloves", Unit = "Box", Quantity = 30, MinimumLimit = 10 }
        };

        await ctrl.Add(vm, null);

        Assert.Equal(130, db.Materials.First().Quantity);
        Assert.Equal("Restock", db.MaterialHistories.First().Note);
    }

    [Fact]
    public async Task Add_WithInvoiceRequested_CreatesInvoiceAndLinksToHistory()
    {
        using var db = CreateDb(nameof(Add_WithInvoiceRequested_CreatesInvoiceAndLinksToHistory));
        var ctrl = CreateController(db);

        var vm = new MaterialsPageViewModel
        {
            NewMaterial = new Material { Name = "Gloves", Unit = "Box", Quantity = 50, MinimumLimit = 5 },
            Invoice = new MaterialInvoiceInput
            {
                CreateInvoice = true,
                UnitPrice = 2.50m,
                PaymentMethod = PaymentMethod.Cash
            }
        };

        await ctrl.Add(vm, null);

        var invoice = Assert.Single(db.MaterialInvoices);
        Assert.Equal(50, invoice.Quantity);
        Assert.Equal(2.50m, invoice.UnitPrice);
        Assert.Equal(125m, invoice.TotalAmount);       // 50 × 2.50
        Assert.Equal(PaymentMethod.Cash, invoice.PaymentMethod);

        // History must be linked to invoice
        var history = Assert.Single(db.MaterialHistories);
        Assert.Equal(invoice.Id, history.MaterialInvoiceId);
    }

    [Fact]
    public async Task Add_InvoiceWithZeroPrice_DoesNotCreateInvoice()
    {
        using var db = CreateDb(nameof(Add_InvoiceWithZeroPrice_DoesNotCreateInvoice));
        var ctrl = CreateController(db);

        var vm = new MaterialsPageViewModel
        {
            NewMaterial = new Material { Name = "Gloves", Unit = "Box", Quantity = 20, MinimumLimit = 2 },
            Invoice = new MaterialInvoiceInput { CreateInvoice = true, UnitPrice = 0 }
        };

        await ctrl.Add(vm, null);

        Assert.Empty(db.MaterialInvoices);
        Assert.Null(db.MaterialHistories.First().MaterialInvoiceId);
    }

    [Fact]
    public async Task Add_InvoiceNotRequested_NoInvoiceCreated()
    {
        using var db = CreateDb(nameof(Add_InvoiceNotRequested_NoInvoiceCreated));
        var ctrl = CreateController(db);

        var vm = new MaterialsPageViewModel
        {
            NewMaterial = new Material { Name = "Gloves", Unit = "Box", Quantity = 20, MinimumLimit = 2 },
            Invoice = new MaterialInvoiceInput { CreateInvoice = false, UnitPrice = 5m }
        };

        await ctrl.Add(vm, null);

        Assert.Empty(db.MaterialInvoices);
    }

    [Fact]
    public async Task Add_WithInvoiceAndImage_UploadsImageAndSavesPath()
    {
        using var db = CreateDb(nameof(Add_WithInvoiceAndImage_UploadsImageAndSavesPath));
        var storageMock = new Mock<IFileStorageService>();
        storageMock.Setup(s => s.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadedFileResult
            {
                ObjectPath = "material-invoices/1/abc.jpg",
                OriginalFileName = "receipt.jpg",
                StoredFileName = "abc.jpg",
                ContentType = "image/jpeg",
                Size = 1024,
                UploadedAtUtc = DateTime.UtcNow
            });

        var ctrl = CreateController(db, storageMock);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("receipt.jpg");
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[1024]));

        var vm = new MaterialsPageViewModel
        {
            NewMaterial = new Material { Name = "Gloves", Unit = "Box", Quantity = 10, MinimumLimit = 2 },
            Invoice = new MaterialInvoiceInput { CreateInvoice = true, UnitPrice = 3m }
        };

        await ctrl.Add(vm, fileMock.Object);

        var invoice = Assert.Single(db.MaterialInvoices);
        Assert.Equal("material-invoices/1/abc.jpg", invoice.InvoiceImageObjectPath);
        storageMock.Verify(s => s.UploadAsync(fileMock.Object, "material-invoices/1", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── UpdateQuantity ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateQuantity_PositiveChange_UpdatesMaterialAndSavesHistory()
    {
        using var db = CreateDb(nameof(UpdateQuantity_PositiveChange_UpdatesMaterialAndSavesHistory));
        SeedMaterial(db, qty: 50);
        var ctrl = CreateController(db);

        var result = await ctrl.UpdateQuantity(1, 20, "Received shipment") as RedirectToActionResult;

        Assert.Equal("Index", result!.ActionName);
        Assert.Equal(70, db.Materials.First().Quantity);
        var h = Assert.Single(db.MaterialHistories);
        Assert.Equal(20, h.QuantityChange);
        Assert.Equal(70, h.NewQuantity);
    }

    [Fact]
    public async Task UpdateQuantity_NegativeChange_ReducesQuantity()
    {
        using var db = CreateDb(nameof(UpdateQuantity_NegativeChange_ReducesQuantity));
        SeedMaterial(db, qty: 50);
        var ctrl = CreateController(db);

        await ctrl.UpdateQuantity(1, -15, null);

        Assert.Equal(35, db.Materials.First().Quantity);
    }

    [Fact]
    public async Task UpdateQuantity_NegativeBeyondZero_ClampsToZero()
    {
        using var db = CreateDb(nameof(UpdateQuantity_NegativeBeyondZero_ClampsToZero));
        SeedMaterial(db, qty: 5);
        var ctrl = CreateController(db);

        await ctrl.UpdateQuantity(1, -100, null);

        Assert.Equal(0, db.Materials.First().Quantity);
    }

    [Fact]
    public async Task UpdateQuantity_WithInvoice_CreatesInvoiceLinkedToHistory()
    {
        using var db = CreateDb(nameof(UpdateQuantity_WithInvoice_CreatesInvoiceLinkedToHistory));
        SeedMaterial(db, qty: 40);
        var ctrl = CreateController(db);

        await ctrl.UpdateQuantity(1, 25, "Restock",
            createInvoice: true, unitPrice: 4m,
            paymentMethod: PaymentMethod.BankTransfer);

        var invoice = Assert.Single(db.MaterialInvoices);
        Assert.Equal(25, invoice.Quantity);
        Assert.Equal(4m, invoice.UnitPrice);
        Assert.Equal(100m, invoice.TotalAmount);
        Assert.Equal(PaymentMethod.BankTransfer, invoice.PaymentMethod);

        var history = Assert.Single(db.MaterialHistories);
        Assert.Equal(invoice.Id, history.MaterialInvoiceId);
    }

    [Fact]
    public async Task UpdateQuantity_NegativeChange_NeverCreatesInvoice()
    {
        using var db = CreateDb(nameof(UpdateQuantity_NegativeChange_NeverCreatesInvoice));
        SeedMaterial(db, qty: 40);
        var ctrl = CreateController(db);

        // Even if createInvoice=true, a negative change must not create an invoice
        await ctrl.UpdateQuantity(1, -10, null,
            createInvoice: true, unitPrice: 5m);

        Assert.Empty(db.MaterialInvoices);
    }

    [Fact]
    public async Task UpdateQuantity_WrongClinic_ReturnsNotFound()
    {
        using var db = CreateDb(nameof(UpdateQuantity_WrongClinic_ReturnsNotFound));
        SeedMaterial(db, clinicId: 99);  // different clinic
        var ctrl = CreateController(db, clinicId: 1);

        var result = await ctrl.UpdateQuantity(1, 10, null);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── Invoice number generation ─────────────────────────────────────────────

    [Fact]
    public async Task GenerateInvoiceNumber_FirstInvoice_EndsIn0001()
    {
        using var db = CreateDb(nameof(GenerateInvoiceNumber_FirstInvoice_EndsIn0001));
        var ctrl = CreateController(db);

        var num = await ctrl.GenerateInvoiceNumberAsync(1);

        Assert.StartsWith("MAT-", num);
        Assert.EndsWith("-0001", num);
    }

    [Fact]
    public async Task GenerateInvoiceNumber_SecondInvoiceInSameMonth_EndsIn0002()
    {
        using var db = CreateDb(nameof(GenerateInvoiceNumber_SecondInvoiceInSameMonth_EndsIn0002));
        var prefix = $"MAT-{DateTime.UtcNow:yyyyMM}-";

        db.MaterialInvoices.Add(new MaterialInvoice
        {
            InvoiceNumber = prefix + "0001",
            ClinicId = 1, MaterialId = 1,
            Quantity = 10, UnitPrice = 1, TotalAmount = 10,
            InvoiceDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var ctrl = CreateController(db);
        var num = await ctrl.GenerateInvoiceNumberAsync(1);

        Assert.EndsWith("-0002", num);
    }

    [Fact]
    public async Task GenerateInvoiceNumber_IsolatedPerClinic()
    {
        using var db = CreateDb(nameof(GenerateInvoiceNumber_IsolatedPerClinic));
        var prefix = $"MAT-{DateTime.UtcNow:yyyyMM}-";

        // Clinic 99 already has 5 invoices — clinic 1 should still start at 0001
        for (int i = 1; i <= 5; i++)
            db.MaterialInvoices.Add(new MaterialInvoice
            {
                InvoiceNumber = prefix + i.ToString("D4"),
                ClinicId = 99, MaterialId = 1,
                Quantity = 1, UnitPrice = 1, TotalAmount = 1,
                InvoiceDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var ctrl = CreateController(db, clinicId: 1);
        var num = await ctrl.GenerateInvoiceNumberAsync(1);

        Assert.EndsWith("-0001", num);
    }

    // ── Invoices list ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Invoices_ReturnsOnlyCurrentClinicInvoices()
    {
        using var db = CreateDb(nameof(Invoices_ReturnsOnlyCurrentClinicInvoices));
        var mat = SeedMaterial(db);

        db.MaterialInvoices.AddRange(
            new MaterialInvoice
            {
                InvoiceNumber = "MAT-202601-0001", ClinicId = 1, MaterialId = mat.Id,
                Quantity = 10, UnitPrice = 2, TotalAmount = 20,
                InvoiceDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
            },
            new MaterialInvoice
            {
                InvoiceNumber = "MAT-202601-0001", ClinicId = 99, MaterialId = mat.Id,
                Quantity = 5, UnitPrice = 1, TotalAmount = 5,
                InvoiceDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var result = await CreateController(db, clinicId: 1).Invoices(null) as ViewResult;
        var vm = Assert.IsType<MaterialInvoicesViewModel>(result!.Model);

        Assert.Single(vm.Invoices);
        Assert.Equal(20m, vm.Invoices[0].TotalAmount);
    }

    [Fact]
    public async Task Invoices_FilterByMaterialId_ReturnsOnlyThatMaterial()
    {
        using var db = CreateDb(nameof(Invoices_FilterByMaterialId_ReturnsOnlyThatMaterial));
        var mat1 = SeedMaterial(db, id: 1, name: "Gloves");
        var mat2 = SeedMaterial(db, id: 2, name: "Masks");

        db.MaterialInvoices.AddRange(
            new MaterialInvoice
            {
                InvoiceNumber = "MAT-202601-0001", ClinicId = 1, MaterialId = mat1.Id,
                Quantity = 10, UnitPrice = 2, TotalAmount = 20,
                InvoiceDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
            },
            new MaterialInvoice
            {
                InvoiceNumber = "MAT-202601-0002", ClinicId = 1, MaterialId = mat2.Id,
                Quantity = 5, UnitPrice = 3, TotalAmount = 15,
                InvoiceDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var result = await CreateController(db).Invoices(mat1.Id) as ViewResult;
        var vm = Assert.IsType<MaterialInvoicesViewModel>(result!.Model);

        Assert.Single(vm.Invoices);
        Assert.Equal(mat1.Id, vm.Invoices[0].MaterialId);
        Assert.Equal(mat1.Id, vm.FilterMaterialId);
    }

    [Fact]
    public async Task Invoices_SummaryTotals_AreCorrect()
    {
        using var db = CreateDb(nameof(Invoices_SummaryTotals_AreCorrect));
        var mat = SeedMaterial(db);
        var now = DateTime.UtcNow;

        db.MaterialInvoices.AddRange(
            new MaterialInvoice  // this month
            {
                InvoiceNumber = "X1", ClinicId = 1, MaterialId = mat.Id,
                Quantity = 1, UnitPrice = 100, TotalAmount = 100,
                InvoiceDate = now, CreatedAt = now
            },
            new MaterialInvoice  // this month
            {
                InvoiceNumber = "X2", ClinicId = 1, MaterialId = mat.Id,
                Quantity = 1, UnitPrice = 50, TotalAmount = 50,
                InvoiceDate = now, CreatedAt = now
            },
            new MaterialInvoice  // last year
            {
                InvoiceNumber = "X3", ClinicId = 1, MaterialId = mat.Id,
                Quantity = 1, UnitPrice = 200, TotalAmount = 200,
                InvoiceDate = now.AddYears(-1), CreatedAt = now.AddYears(-1)
            });
        await db.SaveChangesAsync();

        var result = await CreateController(db).Invoices(null) as ViewResult;
        var vm = Assert.IsType<MaterialInvoicesViewModel>(result!.Model);

        Assert.Equal(150m, vm.TotalThisMonth);
        Assert.Equal(150m, vm.TotalThisYear);
        Assert.Equal(350m, vm.TotalAllTime);
    }

    // ── History endpoint ──────────────────────────────────────────────────────

    [Fact]
    public async Task History_IncludesInvoiceInfo_WhenInvoiceExists()
    {
        using var db = CreateDb(nameof(History_IncludesInvoiceInfo_WhenInvoiceExists));
        var mat = SeedMaterial(db);

        var invoice = new MaterialInvoice
        {
            InvoiceNumber = "MAT-202601-0001", ClinicId = 1, MaterialId = mat.Id,
            Quantity = 10, UnitPrice = 5, TotalAmount = 50,
            InvoiceDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
        };
        db.MaterialInvoices.Add(invoice);
        await db.SaveChangesAsync();

        db.MaterialHistories.Add(new MaterialHistory
        {
            MaterialId = mat.Id, QuantityChange = 10, NewQuantity = 110,
            Note = "Restock", MaterialInvoiceId = invoice.Id
        });
        await db.SaveChangesAsync();

        var result = await CreateController(db).History(mat.Id) as JsonResult;
        Assert.NotNull(result);

        var json = System.Text.Json.JsonSerializer.Serialize(result!.Value);
        Assert.Contains("invoiceNumber", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MAT-202601-0001", json);
    }

    [Fact]
    public async Task History_WrongClinic_ReturnsNotFound()
    {
        using var db = CreateDb(nameof(History_WrongClinic_ReturnsNotFound));
        SeedMaterial(db, clinicId: 99);

        var result = await CreateController(db, clinicId: 1).History(1);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── Delete cascades invoices ──────────────────────────────────────────────

    [Fact]
    public async Task Delete_RemovesMaterialAndItsInvoices()
    {
        using var db = CreateDb(nameof(Delete_RemovesMaterialAndItsInvoices));
        var mat = SeedMaterial(db);

        var invoice = new MaterialInvoice
        {
            InvoiceNumber = "MAT-202601-0001", ClinicId = 1, MaterialId = mat.Id,
            Quantity = 10, UnitPrice = 2, TotalAmount = 20,
            InvoiceDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
        };
        db.MaterialInvoices.Add(invoice);
        await db.SaveChangesAsync();

        db.MaterialHistories.Add(new MaterialHistory
        {
            MaterialId = mat.Id, QuantityChange = 10, NewQuantity = 110,
            Note = "Test", MaterialInvoiceId = invoice.Id
        });
        await db.SaveChangesAsync();

        await CreateController(db).Delete(mat.Id);

        Assert.Empty(db.Materials);
        Assert.Empty(db.MaterialHistories);
        Assert.Empty(db.MaterialInvoices);
    }

    // ── InvoiceImage ──────────────────────────────────────────────────────────

    [Fact]
    public async Task InvoiceImage_NoImagePath_ReturnsNotFound()
    {
        using var db = CreateDb(nameof(InvoiceImage_NoImagePath_ReturnsNotFound));
        var mat = SeedMaterial(db);

        db.MaterialInvoices.Add(new MaterialInvoice
        {
            InvoiceNumber = "X1", ClinicId = 1, MaterialId = mat.Id,
            Quantity = 1, UnitPrice = 1, TotalAmount = 1,
            InvoiceDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
            InvoiceImageObjectPath = null  // no image
        });
        await db.SaveChangesAsync();

        var result = await CreateController(db).InvoiceImage(1, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task InvoiceImage_WrongClinic_ReturnsNotFound()
    {
        using var db = CreateDb(nameof(InvoiceImage_WrongClinic_ReturnsNotFound));
        var mat = SeedMaterial(db, clinicId: 99);

        db.MaterialInvoices.Add(new MaterialInvoice
        {
            InvoiceNumber = "X1", ClinicId = 99, MaterialId = mat.Id,
            Quantity = 1, UnitPrice = 1, TotalAmount = 1,
            InvoiceDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
            InvoiceImageObjectPath = "material-invoices/99/abc.jpg"
        });
        await db.SaveChangesAsync();

        var result = await CreateController(db, clinicId: 1).InvoiceImage(1, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task InvoiceImage_ValidInvoiceWithImage_RedirectsToSignedUrl()
    {
        using var db = CreateDb(nameof(InvoiceImage_ValidInvoiceWithImage_RedirectsToSignedUrl));
        var mat = SeedMaterial(db);

        db.MaterialInvoices.Add(new MaterialInvoice
        {
            InvoiceNumber = "X1", ClinicId = 1, MaterialId = mat.Id,
            Quantity = 1, UnitPrice = 1, TotalAmount = 1,
            InvoiceDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
            InvoiceImageObjectPath = "material-invoices/1/abc.jpg"
        });
        await db.SaveChangesAsync();

        var storageMock = new Mock<IFileStorageService>();
        storageMock
            .Setup(s => s.GenerateSignedReadUrlAsync("material-invoices/1/abc.jpg", It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://signed.example.com/abc.jpg");

        var result = await CreateController(db, storageMock).InvoiceImage(1, CancellationToken.None) as RedirectResult;

        Assert.NotNull(result);
        Assert.Equal("https://signed.example.com/abc.jpg", result!.Url);
    }
}
