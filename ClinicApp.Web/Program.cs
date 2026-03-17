using ClinicApp.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using ClinicApp.Web.Models;
using ClinicApp.Web.Services;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// Enable detailed logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Railway: Configure PORT
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

Console.WriteLine($"=== Application Starting ===");
Console.WriteLine($"Port: {port}");

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// Railway: Database Configuration with detailed logging
try
{
    string connectionString;
    var pgHost = Environment.GetEnvironmentVariable("PGHOST");
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    
    Console.WriteLine($"PGHOST: {pgHost ?? "not set"}");
    Console.WriteLine($"DATABASE_URL: {(string.IsNullOrEmpty(databaseUrl) ? "not set" : "set")}");

    if (!string.IsNullOrEmpty(pgHost))
    {
        // Railway PostgreSQL using individual variables
        var pgPort = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
        var pgDatabase = Environment.GetEnvironmentVariable("PGDATABASE");
        var pgUser = Environment.GetEnvironmentVariable("PGUSER");
        var pgPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
        
        Console.WriteLine($"PostgreSQL Config - Host: {pgHost}, Port: {pgPort}, Database: {pgDatabase}, User: {pgUser}");
        
        connectionString = $"Host={pgHost};" +
                          $"Port={pgPort};" +
                          $"Database={pgDatabase};" +
                          $"Username={pgUser};" +
                          $"Password={pgPassword};" +
                          $"SSL Mode=Require;" +
                          $"Trust Server Certificate=true";
    }
    else if (!string.IsNullOrEmpty(databaseUrl))
    {
        // Parse DATABASE_URL format: postgres://user:password@host:port/database
        Console.WriteLine("Parsing DATABASE_URL...");
        var databaseUri = new Uri(databaseUrl);
        var userInfo = databaseUri.UserInfo.Split(':');
        
        connectionString = $"Host={databaseUri.Host};" +
                          $"Port={databaseUri.Port};" +
                          $"Database={databaseUri.AbsolutePath.TrimStart('/')};" +
                          $"Username={userInfo[0]};" +
                          $"Password={userInfo[1]};" +
                          $"SSL Mode=Require;" +
                          $"Trust Server Certificate=true";
        
        Console.WriteLine($"Parsed connection to host: {databaseUri.Host}");
    }
    else
    {
        // Local development
        connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        Console.WriteLine("Using local connection string");
    }

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString)
    );
    
    Console.WriteLine("DbContext configured successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR configuring database: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    throw;
}

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
ExcelPackage.License.SetNonCommercialOrganization("Sami Shamoon College of Engineering");
builder.Services.AddScoped<IPrintService, PrintService>();
builder.Services.AddScoped<IExportService, ExportService>();

var app = builder.Build();

Console.WriteLine("=== Running Migrations ===");

// Auto-migration & Seed Data
try
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher<AppUser>>();

        Console.WriteLine("Applying migrations...");
       try
        {
           context.Database.Migrate();
         }
         catch (Exception ex)
        {
        Console.WriteLine("Migration failed: " + ex.Message);
        }
        Console.WriteLine("Migrations applied successfully");

        // Seed default data
        if (!context.AppUsers.Any())
        {
            Console.WriteLine("Seeding initial data...");
            
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
            
            Console.WriteLine("Initial data seeded successfully");
        }
        else
        {
            Console.WriteLine("Data already exists, skipping seed");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR during migration/seeding: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
    }
    throw;
}

Console.WriteLine("=== Configuring HTTP Pipeline ===");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

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

Console.WriteLine($"=== Application Ready - Listening on port {port} ===");


app.Run();
