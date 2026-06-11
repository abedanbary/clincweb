// TEMPORARY: This controller is only for testing the file storage service.
// Delete this file once you have integrated IFileStorageService into your real controllers.
using ClinicApp.Web.Services.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicApp.Web.Controllers;

[ApiController]
[Route("api/test-files")]
public class TestFilesController : ControllerBase
{
    private readonly IFileStorageService _storage;

    public TestFilesController(IFileStorageService storage)
    {
        _storage = storage;
    }

    // POST /api/test-files/upload
    [HttpPost("upload")]
    [EnableRateLimiting("upload-limit")]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string folder = "test", CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        try
        {
            var result = await _storage.UploadAsync(file, folder, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET /api/test-files/signed-url?path=...&expiryMinutes=60
    [HttpGet("signed-url")]
    public async Task<IActionResult> GetSignedUrl([FromQuery] string path, [FromQuery] int expiryMinutes = 60, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest(new { error = "Query parameter 'path' is required." });

        try
        {
            var url = await _storage.GenerateSignedReadUrlAsync(path, TimeSpan.FromMinutes(expiryMinutes), cancellationToken);
            return Ok(new { url, expiresInMinutes = expiryMinutes });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // DELETE /api/test-files?path=...
    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest(new { error = "Query parameter 'path' is required." });

        try
        {
            await _storage.DeleteAsync(path, cancellationToken);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
