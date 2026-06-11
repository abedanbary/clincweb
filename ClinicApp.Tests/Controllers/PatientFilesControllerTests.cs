using ClinicApp.Web.Controllers;
using ClinicApp.Web.Models;
using ClinicApp.Web.Services.Storage;
using ClinicApp.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace ClinicApp.Tests.Controllers;

public class PatientFilesControllerTests
{
    // ── Seed helpers ───────────────────────────────────────────────────────────

    private static Patient SeedPatient(int id = 1, int clinicId = 1) => new()
    {
        Id = id, FirstName = "Sara", LastName = "Ali",
        Phone = "050-000-0000", Gender = "Female",
        DateOfBirth = DateTime.UtcNow.AddYears(-25),
        ClinicId = clinicId
    };

    private static PatientFile SeedFile(int id = 10, int patientId = 1,
        string ext = ".jpg", string objectPath = "patients/1/intraoral-photo/2026/01/01/abc.jpg") => new()
    {
        Id = id,
        PatientId = patientId,
        Category = PatientFileCategory.IntraoralPhoto,
        OriginalFileName = $"photo{ext}",
        StoredFileName = $"abc{ext}",
        ObjectPath = objectPath,
        ContentType = "image/jpeg",
        Extension = ext,
        Size = 1024,
        UploadedAtUtc = DateTime.UtcNow
    };

    private static PatientFile SeedDeletedFile(int id = 20, int patientId = 1) => new()
    {
        Id = id, PatientId = patientId,
        Category = PatientFileCategory.Other,
        OriginalFileName = "old.jpg", StoredFileName = "old.jpg",
        ObjectPath = "patients/1/other/2025/01/01/old.jpg",
        ContentType = "image/jpeg", Extension = ".jpg",
        Size = 512, UploadedAtUtc = DateTime.UtcNow.AddDays(-10),
        DeletedAtUtc = DateTime.UtcNow.AddDays(-5) // soft-deleted
    };

    private static IFormFile MakeFile(
        string fileName = "photo.jpg",
        string contentType = "image/jpeg",
        long sizeBytes = 2048)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(sizeBytes);
        mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[sizeBytes]));
        return mock.Object;
    }

    private PatientFilesController CreateController(
        Web.Data.ApplicationDbContext db,
        Mock<IFileStorageService> storageMock,
        int clinicId = 1)
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("ClinicId", clinicId.ToString()),
            new Claim(ClaimTypes.Role, "Doctor")
        }, "TestAuth"));

        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PatientFilesController>();
        var controller = new PatientFilesController(db, storageMock.Object, logger);
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        return controller;
    }

    // ── Index ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Index_PatientNotInClinic_ReturnsNotFound()
    {
        using var db = TestHelpers.CreateDb(nameof(Index_PatientNotInClinic_ReturnsNotFound));
        db.Patients.Add(SeedPatient(clinicId: 99)); // wrong clinic
        await db.SaveChangesAsync();

        var ctrl = CreateController(db, new Mock<IFileStorageService>(), clinicId: 1);
        var result = await ctrl.Index(1, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Index_ValidPatient_ReturnsViewWithNonDeletedFilesOnly()
    {
        using var db = TestHelpers.CreateDb(nameof(Index_ValidPatient_ReturnsViewWithNonDeletedFilesOnly));
        db.Patients.Add(SeedPatient());
        db.PatientFiles.Add(SeedFile(id: 1));
        db.PatientFiles.Add(SeedDeletedFile(id: 2));
        await db.SaveChangesAsync();

        var ctrl = CreateController(db, new Mock<IFileStorageService>());
        var result = await ctrl.Index(1, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<PatientFilesViewModel>(view.Model);
        Assert.Single(vm.Files); // only the non-deleted one
        Assert.Equal(1, vm.Files[0].Id);
    }

    [Fact]
    public async Task Index_NoFiles_ReturnsEmptyList()
    {
        using var db = TestHelpers.CreateDb(nameof(Index_NoFiles_ReturnsEmptyList));
        db.Patients.Add(SeedPatient());
        await db.SaveChangesAsync();

        var ctrl = CreateController(db, new Mock<IFileStorageService>());
        var result = await ctrl.Index(1, CancellationToken.None);

        var vm = Assert.IsType<PatientFilesViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Empty(vm.Files);
    }

    // ── Upload ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_PatientNotInClinic_ReturnsNotFound()
    {
        using var db = TestHelpers.CreateDb(nameof(Upload_PatientNotInClinic_ReturnsNotFound));
        db.Patients.Add(SeedPatient(clinicId: 99));
        await db.SaveChangesAsync();

        var ctrl = CreateController(db, new Mock<IFileStorageService>(), clinicId: 1);
        var result = await ctrl.Upload(1, MakeFile(), PatientFileCategory.IntraoralPhoto, null, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Upload_StorageThrowsArgumentException_SetsFileErrorTempData()
    {
        using var db = TestHelpers.CreateDb(nameof(Upload_StorageThrowsArgumentException_SetsFileErrorTempData));
        db.Patients.Add(SeedPatient());
        await db.SaveChangesAsync();

        var storageMock = new Mock<IFileStorageService>();
        storageMock.Setup(s => s.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("File extension '.exe' is not allowed."));

        var ctrl = CreateController(db, storageMock);
        var result = await ctrl.Upload(1, MakeFile("virus.exe", "application/octet-stream"),
            PatientFileCategory.Other, null, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("File extension '.exe' is not allowed.", ctrl.TempData["FileError"]);
    }

    [Fact]
    public async Task Upload_ValidFile_SavesPatientFileAndRedirects()
    {
        using var db = TestHelpers.CreateDb(nameof(Upload_ValidFile_SavesPatientFileAndRedirects));
        db.Patients.Add(SeedPatient());
        await db.SaveChangesAsync();

        var storageMock = new Mock<IFileStorageService>();
        storageMock.Setup(s => s.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadedFileResult
            {
                OriginalFileName = "xray.jpg",
                StoredFileName = "abc123.jpg",
                ObjectPath = "patients/1/intraoral-photo/2026/06/11/abc123.jpg",
                ContentType = "image/jpeg",
                Size = 2048,
                UploadedAtUtc = DateTime.UtcNow
            });

        var ctrl = CreateController(db, storageMock);
        var result = await ctrl.Upload(1, MakeFile("xray.jpg"), PatientFileCategory.IntraoralPhoto, "Pre-treatment", CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var saved = await db.PatientFiles.SingleAsync();
        Assert.Equal("xray.jpg", saved.OriginalFileName);
        Assert.Equal(".jpg", saved.Extension);
        Assert.Equal(PatientFileCategory.IntraoralPhoto, saved.Category);
        Assert.Equal("Pre-treatment", saved.Notes);
        Assert.Null(saved.DeletedAtUtc);
    }

    [Fact]
    public async Task Upload_NotesWhitespaceOnly_SavedAsNull()
    {
        using var db = TestHelpers.CreateDb(nameof(Upload_NotesWhitespaceOnly_SavedAsNull));
        db.Patients.Add(SeedPatient());
        await db.SaveChangesAsync();

        var storageMock = new Mock<IFileStorageService>();
        storageMock.Setup(s => s.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadedFileResult
            {
                OriginalFileName = "scan.jpg", StoredFileName = "abc.jpg",
                ObjectPath = "patients/1/other/2026/06/11/abc.jpg",
                ContentType = "image/jpeg", Size = 1024, UploadedAtUtc = DateTime.UtcNow
            });

        var ctrl = CreateController(db, storageMock);
        await ctrl.Upload(1, MakeFile(), PatientFileCategory.Other, "   ", CancellationToken.None);

        var saved = await db.PatientFiles.SingleAsync();
        Assert.Null(saved.Notes);
    }

    [Fact]
    public async Task Upload_UsesCorrectFolderBasedOnCategory()
    {
        using var db = TestHelpers.CreateDb(nameof(Upload_UsesCorrectFolderBasedOnCategory));
        db.Patients.Add(SeedPatient());
        await db.SaveChangesAsync();

        string? capturedFolder = null;
        var storageMock = new Mock<IFileStorageService>();
        storageMock.Setup(s => s.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<IFormFile, string, CancellationToken>((_, folder, _) => capturedFolder = folder)
            .ReturnsAsync(new UploadedFileResult
            {
                OriginalFileName = "scan.stl", StoredFileName = "abc.stl",
                ObjectPath = "patients/1/intraoral-scan/2026/06/11/abc.stl",
                ContentType = "model/stl", Size = 5 * 1024 * 1024, UploadedAtUtc = DateTime.UtcNow
            });

        var ctrl = CreateController(db, storageMock);
        await ctrl.Upload(1, MakeFile("scan.stl", "model/stl"), PatientFileCategory.IntraoralScan, null, CancellationToken.None);

        Assert.Equal("patients/1/intraoral-scan", capturedFolder);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_FileNotFound_ReturnsNotFound()
    {
        using var db = TestHelpers.CreateDb(nameof(Delete_FileNotFound_ReturnsNotFound));
        db.Patients.Add(SeedPatient());
        await db.SaveChangesAsync();

        var ctrl = CreateController(db, new Mock<IFileStorageService>());
        var result = await ctrl.Delete(1, fileId: 999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_ValidFile_SetsDeletedAtUtcAndRedirects()
    {
        using var db = TestHelpers.CreateDb(nameof(Delete_ValidFile_SetsDeletedAtUtcAndRedirects));
        db.Patients.Add(SeedPatient());
        db.PatientFiles.Add(SeedFile());
        await db.SaveChangesAsync();

        var storageMock = new Mock<IFileStorageService>();
        storageMock.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var ctrl = CreateController(db, storageMock);
        var result = await ctrl.Delete(patientId: 1, fileId: 10, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var file = await db.PatientFiles.FindAsync(10);
        Assert.NotNull(file!.DeletedAtUtc); // soft-deleted
    }

    [Fact]
    public async Task Delete_GcsThrows_StillSoftDeletesDbRecord()
    {
        using var db = TestHelpers.CreateDb(nameof(Delete_GcsThrows_StillSoftDeletesDbRecord));
        db.Patients.Add(SeedPatient());
        db.PatientFiles.Add(SeedFile());
        await db.SaveChangesAsync();

        var storageMock = new Mock<IFileStorageService>();
        storageMock.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("GCS unavailable"));

        var ctrl = CreateController(db, storageMock);
        await ctrl.Delete(patientId: 1, fileId: 10, CancellationToken.None);

        var file = await db.PatientFiles.FindAsync(10);
        Assert.NotNull(file!.DeletedAtUtc); // must still be soft-deleted despite GCS error
    }

    [Fact]
    public async Task Delete_AlreadySoftDeleted_ReturnsNotFound()
    {
        using var db = TestHelpers.CreateDb(nameof(Delete_AlreadySoftDeleted_ReturnsNotFound));
        db.Patients.Add(SeedPatient());
        db.PatientFiles.Add(SeedDeletedFile());
        await db.SaveChangesAsync();

        var ctrl = CreateController(db, new Mock<IFileStorageService>());
        var result = await ctrl.Delete(patientId: 1, fileId: 20, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_FileFromDifferentClinic_ReturnsNotFound()
    {
        using var db = TestHelpers.CreateDb(nameof(Delete_FileFromDifferentClinic_ReturnsNotFound));
        db.Patients.Add(SeedPatient(clinicId: 99)); // different clinic
        db.PatientFiles.Add(SeedFile());
        await db.SaveChangesAsync();

        var ctrl = CreateController(db, new Mock<IFileStorageService>(), clinicId: 1);
        var result = await ctrl.Delete(patientId: 1, fileId: 10, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── SignedUrl ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SignedUrl_FileNotFound_ReturnsNotFound()
    {
        using var db = TestHelpers.CreateDb(nameof(SignedUrl_FileNotFound_ReturnsNotFound));
        db.Patients.Add(SeedPatient());
        await db.SaveChangesAsync();

        var ctrl = CreateController(db, new Mock<IFileStorageService>());
        var result = await ctrl.SignedUrl(patientId: 1, fileId: 999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task SignedUrl_ValidFile_ReturnsUrlJson()
    {
        using var db = TestHelpers.CreateDb(nameof(SignedUrl_ValidFile_ReturnsUrlJson));
        db.Patients.Add(SeedPatient());
        db.PatientFiles.Add(SeedFile());
        await db.SaveChangesAsync();

        var storageMock = new Mock<IFileStorageService>();
        storageMock.Setup(s => s.GenerateSignedReadUrlAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage.googleapis.com/signed-url-here");

        var ctrl = CreateController(db, storageMock);
        var result = await ctrl.SignedUrl(patientId: 1, fileId: 10, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        Assert.Contains("signed-url-here", json);
    }

    [Fact]
    public async Task SignedUrl_SoftDeletedFile_ReturnsNotFound()
    {
        using var db = TestHelpers.CreateDb(nameof(SignedUrl_SoftDeletedFile_ReturnsNotFound));
        db.Patients.Add(SeedPatient());
        db.PatientFiles.Add(SeedDeletedFile());
        await db.SaveChangesAsync();

        var ctrl = CreateController(db, new Mock<IFileStorageService>());
        var result = await ctrl.SignedUrl(patientId: 1, fileId: 20, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── Viewer ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Viewer_NonThreeDFile_ReturnsBadRequest()
    {
        using var db = TestHelpers.CreateDb(nameof(Viewer_NonThreeDFile_ReturnsBadRequest));
        db.Patients.Add(SeedPatient());
        db.PatientFiles.Add(SeedFile(ext: ".jpg")); // not a 3D file
        await db.SaveChangesAsync();

        var ctrl = CreateController(db, new Mock<IFileStorageService>());
        var result = await ctrl.Viewer(patientId: 1, fileId: 10, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData(".stl")]
    [InlineData(".obj")]
    [InlineData(".ply")]
    [InlineData(".glb")]
    [InlineData(".gltf")]
    public async Task Viewer_ThreeDFile_ReturnsView(string ext)
    {
        var dbName = $"{nameof(Viewer_ThreeDFile_ReturnsView)}_{ext}";
        using var db = TestHelpers.CreateDb(dbName);
        db.Patients.Add(SeedPatient());
        db.PatientFiles.Add(SeedFile(ext: ext, objectPath: $"patients/1/intraoral-scan/2026/01/01/abc{ext}"));
        await db.SaveChangesAsync();

        var ctrl = CreateController(db, new Mock<IFileStorageService>());
        var result = await ctrl.Viewer(patientId: 1, fileId: 10, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<PatientFileViewerViewModel>(view.Model);
        Assert.Equal(ext, vm.Extension);
        Assert.Equal(1, vm.PatientId);
        Assert.Equal(10, vm.FileId);
    }

    [Fact]
    public async Task Viewer_FileNotFound_ReturnsNotFound()
    {
        using var db = TestHelpers.CreateDb(nameof(Viewer_FileNotFound_ReturnsNotFound));
        db.Patients.Add(SeedPatient());
        await db.SaveChangesAsync();

        var ctrl = CreateController(db, new Mock<IFileStorageService>());
        var result = await ctrl.Viewer(patientId: 1, fileId: 999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
