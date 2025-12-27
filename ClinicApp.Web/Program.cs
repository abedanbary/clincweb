using ClinicApp.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using ClinicApp.Web.Models;
using ClinicApp.Web.Services;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// ✅ Railway: Configure PORT
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// ✅ Railway: Database Configuration
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
if (string.IsNullOrEmpty(connectionString))
{
    // Local development
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// hash password
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

// cookie auth
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
    });

builder.Services.AddAuthorization();
ExcelPackage.License.SetNonCommercialOrganization("ClinicApp");
builder.Services.AddScoped<IPrintService, PrintService>();
builder.Services.AddScoped<IExportService, ExportService>();

var app = builder.Build();

// ✅ Auto-migration & Seed Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher<AppUser>>();

        // Apply migrations
        context.Database.Migrate();

        // Seed default data
        if (!context.AppUsers.Any())
        {
            var clinic = new Clinic
            {
                Name = "Main Clinic",
                Address = "Haifa"
            };
            context.Clinics.Add(clinic);
            context.SaveChanges();

            var manager = new AppUser
            {
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@clinic.com",
                Phone = "0500000000",
                ClinicId = clinic.Id,
                Role = UserRole.Manager,
            };
            manager.PasswordHash = hasher.HashPassword(manager, "123456");

            context.AppUsers.Add(manager);
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ✅ Railway: Remove HTTPS redirect in production (Railway handles this)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();