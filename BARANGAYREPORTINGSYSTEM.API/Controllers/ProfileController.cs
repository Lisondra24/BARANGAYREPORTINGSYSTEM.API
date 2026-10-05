using Dapper;
using BarangayReportingAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;

namespace BarangayReportingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "User")]
    public class ProfileController : ControllerBase
    {
        private readonly IConfiguration _cfg;
        private readonly IWebHostEnvironment _env;
        public ProfileController(IConfiguration cfg, IWebHostEnvironment env) { _cfg = cfg; _env = env; }
        private SqlConnection Db() => new SqlConnection(_cfg["ConnectionStrings:BarangayReportingDb"]);
        private int Uid => int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);

        [HttpGet]
        public async Task<IActionResult> Me()
        {
            using var db = Db();
            var row = await db.QueryFirstOrDefaultAsync(@"
                SELECT u.UserId, u.FullName, u.Email, u.Username, u.ContactNumber, u.Age, u.Gender, u.Address,
                       u.ProfilePicturePath, u.CreatedAt, p.PurokName
                FROM dbo.Users u LEFT JOIN dbo.Purok p ON p.PurokId = u.PurokId WHERE u.UserId = @u", new { u = Uid });
            return Ok(new { Status = 200, Data = row });
        }

        // Public view of another resident: name, photo, join date only
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Public(int id)
        {
            using var db = Db();
            var row = await db.QueryFirstOrDefaultAsync(
                "SELECT UserId, FullName, ProfilePicturePath, CreatedAt FROM dbo.Users WHERE UserId = @id AND IsDeleted = 0", new { id });
            return row == null ? NotFound(new { Status = 404, Message = "Not found." }) : Ok(new { Status = 200, Data = row });
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateProfileRequest r)
        {
            if (string.IsNullOrWhiteSpace(r.FullName)) return BadRequest(new { Status = 400, Message = "Full name is required." });
            using var db = Db();
            await db.ExecuteAsync(@"UPDATE dbo.Users SET FullName = @FullName, Age = @Age, Gender = @Gender,
                                    Address = @Address, ContactNumber = @ContactNumber WHERE UserId = @u",
                new { r.FullName, r.Age, r.Gender, r.Address, r.ContactNumber, u = Uid });
            return Ok(new { Status = 200, Message = "Profile updated." });
        }

        [HttpPost("photo")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Photo([FromForm] ProfilePhotoRequest request)
        {
            var photo = request.Photo;
            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (photo.Length == 0 || photo.Length > 5 * 1024 * 1024 || ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                return BadRequest(new { Status = 400, Message = "Use a JPG, PNG or WEBP under 5 MB." });

            var folder = Path.Combine(_env.WebRootPath, "uploads", "profiles", Uid.ToString());
            Directory.CreateDirectory(folder);
            var name = $"{Guid.NewGuid()}{ext}";
            using (var fs = new FileStream(Path.Combine(folder, name), FileMode.Create)) await photo.CopyToAsync(fs);

            var rel = $"uploads/profiles/{Uid}/{name}";
            using var db = Db();
            await db.ExecuteAsync("UPDATE dbo.Users SET ProfilePicturePath = @rel WHERE UserId = @u", new { rel, u = Uid });
            return Ok(new { Status = 200, Message = "Photo updated.", Data = new { ProfilePicturePath = rel } });
        }

        [HttpPost("password")]
        public async Task<IActionResult> Password([FromBody] ChangePasswordRequest r)
        {
            if (r.NewPassword.Length < 8) return BadRequest(new { Status = 400, Message = "New password must be at least 8 characters." });
            using var db = Db();
            var hash = await db.QueryFirstOrDefaultAsync<string>("SELECT PasswordHash FROM dbo.Users WHERE UserId = @u", new { u = Uid });
            if (hash == null || !BCrypt.Net.BCrypt.Verify(r.CurrentPassword, hash))
                return BadRequest(new { Status = 400, Message = "Current password is incorrect." });

            await db.ExecuteAsync("UPDATE dbo.Users SET PasswordHash = @h WHERE UserId = @u",
                new { h = BCrypt.Net.BCrypt.HashPassword(r.NewPassword, workFactor: 12), u = Uid });
            return Ok(new { Status = 200, Message = "Password changed." });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteMe()
        {
            using var db = Db();
            await db.ExecuteAsync("UPDATE dbo.Users SET IsDeleted = 1, DeletedAt = GETDATE() WHERE UserId = @u", new { u = Uid });
            return Ok(new { Status = 200, Message = "Account deleted." });
        }
    }

    public class ProfilePhotoRequest
    {
        public IFormFile Photo { get; set; } = null!;
    }
}