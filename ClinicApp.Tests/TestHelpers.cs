using ClinicApp.Web.Data;
using ClinicApp.Web.Models;
using ClinicApp.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace ClinicApp.Tests;

public static class TestHelpers
{
    public static ApplicationDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new ApplicationDbContext(options);
    }

    public static ClinicApp.Web.Controllers.PaymentsController CreatePaymentsController(
        ApplicationDbContext db,
        int clinicId = 1,
        int userId = 10,
        UserRole role = UserRole.Manager)
    {
        var controller = new ClinicApp.Web.Controllers.PaymentsController(db);
        var http = new DefaultHttpContext();

        var claims = new[]
        {
            new Claim("ClinicId", clinicId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role.ToString())
        };

        http.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        return controller;
    }

    public static ClinicApp.Web.Controllers.AttendanceController CreateAttendanceController(
        ApplicationDbContext db,
        int clinicId = 1,
        int userId = 10,
        UserRole role = UserRole.Manager)
    {
        var controller = new ClinicApp.Web.Controllers.AttendanceController(db);
        var http = new DefaultHttpContext();

        var claims = new List<Claim>
        {
            new Claim("ClinicId", clinicId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role.ToString())
        };

        if (role == UserRole.Doctor)
            claims.Add(new Claim("FirstName", "Test"));

        http.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        return controller;
    }

    public static ClinicApp.Web.Controllers.AppointmentsController CreateAppointmentsController(
        ApplicationDbContext db,
        int clinicId = 1,
        int userId = 10,
        UserRole role = UserRole.Manager,
        string? referer = null,
        Mock<IPrintService>? printMock = null,
        Mock<IExportService>? exportMock = null)
    {
        printMock ??= new Mock<IPrintService>();
        exportMock ??= new Mock<IExportService>();

        var controller = new ClinicApp.Web.Controllers.AppointmentsController(
            db, printMock.Object, exportMock.Object);

        var http = new DefaultHttpContext();

        if (!string.IsNullOrEmpty(referer))
            http.Request.Headers["Referer"] = referer;

        var claims = new[]
        {
            new Claim("ClinicId", clinicId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role.ToString())
        };

        http.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        return controller;
    }
}
