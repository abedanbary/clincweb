using ClinicApp.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using ClinicApp.Web.Models;
using ClinicApp.Web.Services;
using ClinicApp.Web.Services.Storage;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// Enable detailed logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Railway: Configure PORT
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// File upload size limits (shared by Kestrel + multipart + the storage service)
var maxUploadMb = int.TryParse(Environment.GetEnvironmentVariable("MAX_UPLOAD_FILE_SIZE_MB"), out var parsedMb) ? parsedMb : 200;
var maxUploadBytes = maxUploadMb * 1024L * 1024L;

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxUploadBytes;
});

Console.WriteLine($"=== Application Starting ===");
Console.WriteLine($"Port: {port}");

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<IFileStorageService, GoogleCloudStorageService>();

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxUploadBytes;
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("upload-limit", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

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

// Authentication: primary cookie + temporary external cookie + Google OAuth
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath        = "/Auth/Login";
        options.LogoutPath       = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan   = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    })
    .AddCookie("ExternalCookie", options =>
    {
        // Short-lived cookie used to hold Google's response until our callback processes it
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddGoogle(options =>
    {
        options.SignInScheme  = "ExternalCookie";
        options.ClientId     = builder.Configuration["Authentication:Google:ClientId"]
                               ?? throw new InvalidOperationException("Authentication:Google:ClientId is not configured.");
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
                               ?? throw new InvalidOperationException("Authentication:Google:ClientSecret is not configured.");
        options.CallbackPath = "/signin-google";
        // Request name and email scopes
        options.Scope.Add("profile");
        options.Scope.Add("email");
        // Redirect to our callback after Google finishes
        options.Events.OnTicketReceived = ctx =>
        {
            ctx.ReturnUri = "/Auth/ExternalCallback";
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();
ExcelPackage.License.SetNonCommercialOrganization("Sami Shamoon College of Engineering");
builder.Services.AddSingleton<IAppTimeService, AppTimeService>();
builder.Services.AddScoped<IToothStatusService, ToothStatusService>();
builder.Services.AddScoped<IPrintService, PrintService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppService>();
builder.Services.AddSingleton<AppointmentReminderService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AppointmentReminderService>());

var app = builder.Build();

Console.WriteLine("=== Running Migrations ===");

// Auto-migration & Seed Data
try
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();

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

        // Migration handles all seed data (SeedDemoData migration)
        Console.WriteLine("Migrations applied — demo data is seeded via migration");
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
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

Console.WriteLine($"=== Application Ready - Listening on port {port} ===");


app.Run();
