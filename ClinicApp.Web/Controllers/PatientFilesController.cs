using ClinicApp.Web.Data;
using ClinicApp.Web.Models;
using ClinicApp.Web.Services.Storage;
using ClinicApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Web.Controllers;

[Authorize(Roles = "Doctor")]
[Route("patients/{patientId}/files")]
public class PatientFilesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storage;
    private readonly ILogger<PatientFilesController> _logger;

    public PatientFilesController(
        ApplicationDbContext context,
        IFileStorageService storage,
        ILogger<PatientFilesController> logger)
    {
        _context = context;
        _storage = storage;
        _logger = logger;
    }

    private int GetCurrentClinicId()
    {
        var claim = User.FindFirst("ClinicId")
            ?? throw new InvalidOperationException("ClinicId claim is missing.");
        return int.Parse(claim.Value);
    }

    private async Task<Patient?> GetPatientAsync(int patientId, int clinicId, CancellationToken ct) =>
        await _context.Patients.FirstOrDefaultAsync(p => p.Id == patientId && p.ClinicId == clinicId, ct);

    private async Task<PatientFile?> GetFileAsync(int patientId, int fileId, CancellationToken ct)
    {
        var clinicId = GetCurrentClinicId();
        return await _context.PatientFiles
            .Include(f => f.Patient)
            .FirstOrDefaultAsync(f =>
                f.Id == fileId &&
                f.PatientId == patientId &&
                f.Patient.ClinicId == clinicId &&
                f.DeletedAtUtc == null, ct);
    }

    // GET /patients/{patientId}/files
    [HttpGet("")]
    public async Task<IActionResult> Index(int patientId, CancellationToken ct)
    {
        var clinicId = GetCurrentClinicId();
        var patient = await GetPatientAsync(patientId, clinicId, ct);
        if (patient == null) return NotFound();

        var files = await _context.PatientFiles
            .Where(f => f.PatientId == patientId && f.DeletedAtUtc == null)
            .OrderByDescending(f => f.UploadedAtUtc)
            .ToListAsync(ct);

        var vm = new PatientFilesViewModel
        {
            PatientId = patientId,
            PatientFirstName = patient.FirstName,
            PatientLastName = patient.LastName,
            Files = files,
            UploadError = TempData["FileError"] as string
        };

        ViewData["Title"] = $"Files — {patient.FirstName} {patient.LastName}";
        return View(vm);
    }

    // POST /patients/{patientId}/files/upload
    [HttpPost("upload")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("upload-limit")]
    public async Task<IActionResult> Upload(
        int patientId,
        IFormFile file,
        PatientFileCategory category,
        string? notes,
        CancellationToken ct)
    {
        var clinicId = GetCurrentClinicId();
        var patient = await GetPatientAsync(patientId, clinicId, ct);
        if (patient == null) return NotFound();

        var folder = $"patients/{patientId}/{PatientFileHelper.GetCategorySlug(category)}";

        try
        {
            var result = await _storage.UploadAsync(file, folder, ct);
            var extension = Path.GetExtension(result.StoredFileName).ToLowerInvariant();

            var patientFile = new PatientFile
            {
                PatientId = patientId,
                Category = category,
                OriginalFileName = result.OriginalFileName,
                StoredFileName = result.StoredFileName,
                ObjectPath = result.ObjectPath,
                ContentType = result.ContentType,
                Extension = extension,
                Size = result.Size,
                UploadedAtUtc = result.UploadedAtUtc,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            };

            _context.PatientFiles.Add(patientFile);
            await _context.SaveChangesAsync(ct);
        }
        catch (ArgumentException ex)
        {
            TempData["FileError"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { patientId });
    }

    // GET /patients/{patientId}/files/{fileId}/signed-url  →  JSON { url }
    [HttpGet("{fileId}/signed-url")]
    public async Task<IActionResult> SignedUrl(int patientId, int fileId, CancellationToken ct)
    {
        var file = await GetFileAsync(patientId, fileId, ct);
        if (file == null) return NotFound();

        var url = await _storage.GenerateSignedReadUrlAsync(file.ObjectPath, TimeSpan.FromMinutes(20), ct);
        return Ok(new { url });
    }

    // POST /patients/{patientId}/files/{fileId}/delete  (soft delete + GCS delete)
    [HttpPost("{fileId}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int patientId, int fileId, CancellationToken ct)
    {
        var file = await GetFileAsync(patientId, fileId, ct);
        if (file == null) return NotFound();

        try
        {
            await _storage.DeleteAsync(file.ObjectPath, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GCS delete failed for file {FileId} path {Path}", fileId, file.ObjectPath);
        }

        file.DeletedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return RedirectToAction(nameof(Index), new { patientId });
    }

    // GET /patients/{patientId}/files/{fileId}/viewer
    [HttpGet("{fileId}/viewer")]
    public async Task<IActionResult> Viewer(int patientId, int fileId, CancellationToken ct)
    {
        var file = await GetFileAsync(patientId, fileId, ct);
        if (file == null) return NotFound();

        if (!PatientFileHelper.IsThreeDModel(file.Extension))
            return BadRequest("This file is not a supported 3D model format.");

        var clinicId = GetCurrentClinicId();
        var patient = await GetPatientAsync(patientId, clinicId, ct);

        var vm = new PatientFileViewerViewModel
        {
            PatientId = patientId,
            FileId = fileId,
            OriginalFileName = file.OriginalFileName,
            Extension = file.Extension,
            Category = file.Category,
            Size = file.Size,
            UploadedAtUtc = file.UploadedAtUtc,
            Notes = file.Notes,
            PatientFirstName = patient?.FirstName ?? "",
            PatientLastName = patient?.LastName ?? ""
        };

        ViewData["Title"] = $"3D Viewer — {file.OriginalFileName}";
        return View(vm);
    }

    // GET /patients/{patientId}/files/{fileId}/stream  (safe backend proxy — used by 3D viewer)
    [HttpGet("{fileId}/stream")]
    public async Task<IActionResult> Stream(int patientId, int fileId, CancellationToken ct)
    {
        var file = await GetFileAsync(patientId, fileId, ct);
        if (file == null) return NotFound();

        Response.Headers["Cache-Control"] = "private, max-age=300";
        Response.Headers["Content-Disposition"] = $"inline; filename=\"{Uri.EscapeDataString(file.OriginalFileName)}\"";
        Response.ContentType = file.ContentType;

        await _storage.StreamToAsync(file.ObjectPath, Response.Body, ct);
        return new EmptyResult();
    }
}
