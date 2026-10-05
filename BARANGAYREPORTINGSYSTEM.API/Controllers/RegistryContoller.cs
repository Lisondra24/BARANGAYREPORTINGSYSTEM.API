using System.Data.SqlClient;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarangayReportingAPI.Controllers
{
    public class AddResidentRequest
    {
        public string? FullName { get; set; }
        public int PurokId { get; set; }
        public string? Gender { get; set; }
        public int BirthYear { get; set; }
    }

    [ApiController]
    [Route("api/Admin/registry")]
    [Authorize(Roles = "Admin")]
    public class RegistryController : ControllerBase
    {
        private readonly string _connStr;
        private readonly IWebHostEnvironment _env;

        public RegistryController(IConfiguration config, IWebHostEnvironment env)
        {
            _connStr = config["ConnectionStrings:BarangayReportingDb"]!;
            _env = env;
        }

        [HttpGet("puroks")]
        public async Task<IActionResult> GetPuroks()
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                var rows = await conn.QueryAsync(
                    "SELECT PurokId AS purokId, PurokName AS purokName FROM dbo.Purok ORDER BY PurokName");
                return Ok(new { status = 200, message = "Success", data = rows });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 500, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddResident([FromBody] AddResidentRequest req)
        {
            try
            {
                var name = string.Join(' ', (req.FullName ?? "")
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

                if (name.Length < 3)
                    return BadRequest(new { status = 400, message = "Enter the resident's full name." });
                if (string.IsNullOrWhiteSpace(req.Gender))
                    return BadRequest(new { status = 400, message = "Select a gender." });
                if (req.BirthYear < 1900 || req.BirthYear > DateTime.Now.Year)
                    return BadRequest(new { status = 400, message = "Enter a valid birth year." });

                using var conn = new SqlConnection(_connStr);

                var purokOk = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM dbo.Purok WHERE PurokId = @PurokId", new { req.PurokId });
                if (purokOk == 0)
                    return BadRequest(new { status = 400, message = "Select a valid purok." });

                var exists = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM dbo.Residents WHERE FullName = @name AND PurokId = @PurokId",
                    new { name, req.PurokId });
                if (exists > 0)
                    return Conflict(new { status = 409, message = "This resident is already in the registry." });

                await conn.ExecuteAsync(
                    @"INSERT INTO dbo.Residents (FullName, PurokId, Gender, BirthYear)
                      VALUES (@name, @PurokId, @Gender, @BirthYear)",
                    new { name, req.PurokId, Gender = req.Gender!.Trim(), req.BirthYear });

                return StatusCode(201, new { status = 201, message = "Resident added.", data = new { fullName = name } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 500, message = ex.Message });
            }
        }

        // Streams a private ID photo (front | back | selfie) to the admin.
        [HttpGet("id-image/{userId:int}/{type}")]
        public async Task<IActionResult> GetIdImage(int userId, string type)
        {
            type = (type ?? "").ToLowerInvariant();
            if (type != "front" && type != "back" && type != "selfie")
                return BadRequest(new { status = 400, message = "Invalid image type." });

            using var conn = new SqlConnection(_connStr);
            var row = await conn.QueryFirstOrDefaultAsync("SELECT * FROM dbo.Users WHERE UserId = @userId", new { userId });
            if (row == null)
                return NotFound(new { status = 404, message = "User not found." });

            // Finds the column by name (contains front / back / selfie), preferring ones with "Path".
            var cols = (IDictionary<string, object>)row;
            string? stored = cols
                .Where(kv => kv.Key.Contains(type, StringComparison.OrdinalIgnoreCase)
                             && kv.Value is string s && !string.IsNullOrWhiteSpace(s))
                .OrderByDescending(kv => kv.Key.Contains("Path", StringComparison.OrdinalIgnoreCase))
                .Select(kv => (string)kv.Value)
                .FirstOrDefault();
            if (stored == null)
                return NotFound(new { status = 404, message = "No image on file." });

            var root = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "PrivateUploads"));
            var rel = stored.Replace('\\', '/').TrimStart('/');
            if (rel.StartsWith("PrivateUploads/", StringComparison.OrdinalIgnoreCase))
                rel = rel.Substring("PrivateUploads/".Length);

            var full = Path.GetFullPath(Path.Combine(root, rel));
            if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || !System.IO.File.Exists(full))
                return NotFound(new { status = 404, message = "Image file not found." });

            var contentType = Path.GetExtension(full).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => null
            };
            if (contentType == null)
                return NotFound(new { status = 404, message = "Unsupported image type." });

            return PhysicalFile(full, contentType);
        }
    }
}