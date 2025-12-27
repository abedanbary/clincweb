using ClinicApp.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using ClinicApp.Web.Models;
using ClinicApp.Web.Services;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);
// hash bassword
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
// cookie auth
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";         // لو مش مسجّل دخول → يروح لهنا
        options.LogoutPath = "/Auth/Logout";       // عنوان تسجيل الخروج
        options.AccessDeniedPath = "/Auth/AccessDenied"; // لو ما عنده صلاحية
    });
builder.Services.AddAuthorization();
ExcelPackage.License.SetNonCommercialOrganization("ClinicApp");
builder.Services.AddScoped<IPrintService, PrintService>();
builder.Services.AddScoped<IExportService, ExportService>();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var hasher = services.GetRequiredService<IPasswordHasher<AppUser>>();

    // تطبق المايجريشن تلقائياً
    context.Database.Migrate();

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
         var hasher1 = services.GetRequiredService<IPasswordHasher<AppUser>>();
         manager.PasswordHash = hasher1.HashPassword(manager, "123456");



        context.AppUsers.Add(manager);
        context.SaveChanges();
    }
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
