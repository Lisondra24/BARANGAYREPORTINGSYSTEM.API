using Dapper;
using BarangayReportingAPI.Model;
using BARANGAYREPORTINGSYSTEM.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BarangayReportingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IConfiguration _cfg;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _email;

        public AdminController(IConfiguration cfg, IWebHostEnvironment env, IEmailService email)
        {
            _cfg = cfg;
            _env = env;
            _email = email;
        }

        private SqlConnection Db() => new SqlConnection(_cfg["ConnectionStrings:BarangayReportingDb"]);
        private IActionResult Done(object? data = null, string msg = "Success") => Ok(new { Status = 200, Message = msg, Data = data });

        private class ComplaintOwner
        {
            public string FullName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Status { get; set; } = "";
        }

        // ---------- admin login ----------
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestModel req)
        {
            using var db = Db();
            var a = await db.QueryFirstOrDefaultAsync<AdminRow>(
                "SP_BARANGAYREPORTING_ADMINLOGIN", new { EmailOrUsername = req.EmailOrUsername },
                commandType: CommandType.StoredProcedure);

            if (a == null || !BCrypt.Net.BCrypt.Verify(req.Password, a.PasswordHash))
                return BadRequest(new { Status = 400, Message = "Invalid Email/Username or Password" });

            var jwt = _cfg.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SecretKey"]!));
            var token = new JwtSecurityToken(jwt["Issuer"], jwt["Audience"],
                new[] {
                    new Claim(JwtRegisteredClaimNames.Sub, a.AdminId.ToString()),
                    new Claim("fullName", a.FullName),
                    new Claim("role", "Admin")
                },
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwt["ExpiryMinutes"])),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return Ok(new
            {
                Status = 200,
                Message = "Login successful.",
                Data = new { a.AdminId, a.FullName, a.Email, a.Username, a.ProfilePicturePath },
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            });
        }

        // ---------- dashboard stats ----------
        [HttpGet("stats")]
        public async Task<IActionResult> Stats()
        {
            using var db = Db();
            var counts = await db.QueryFirstAsync(@"
                SELECT (SELECT COUNT(*) FROM dbo.Complaints)                                   AS Total,
                       (SELECT COUNT(*) FROM dbo.Complaints WHERE Status = 'Resolved')         AS Resolved,
                       (SELECT COUNT(*) FROM dbo.Complaints WHERE Status = 'Pending')          AS Pending,
                       (SELECT COUNT(*) FROM dbo.Users WHERE VerificationStatus = 'Approved' AND IsDeleted = 0) AS RegisteredUsers");
            var week = await db.QueryAsync(@"
                SELECT CAST(CreatedAt AS DATE) AS Day, COUNT(*) AS Count FROM dbo.Complaints
                WHERE CreatedAt >= DATEADD(DAY, -6, CAST(GETDATE() AS DATE))
                GROUP BY CAST(CreatedAt AS DATE)");
            var recent = await db.QueryAsync(@"
                SELECT TOP 5 u.FullName, c.Status, COALESCE(c.ResolvedAt, c.CreatedAt) AS At
                FROM dbo.Complaints c JOIN dbo.Users u ON u.UserId = c.UserId
                ORDER BY COALESCE(c.ResolvedAt, c.CreatedAt) DESC");
            var newest = await db.QueryAsync(@"
                SELECT TOP 5 FullName, ProfilePicturePath, CreatedAt FROM dbo.Users
                WHERE IsDeleted = 0 ORDER BY UserId DESC");
            return Done(new { counts, week, recent, newest });
        }

        // ---------- complaints (with email) + resolve ----------
        [HttpGet("complaints")]
        public async Task<IActionResult> Complaints()
        {
            using var db = Db();
            var rows = await db.QueryAsync(@"
                SELECT c.ComplaintId, c.UserId, u.FullName, u.Email, u.ProfilePicturePath,
                       c.ComplaintText, c.Status, c.CreatedAt
                FROM dbo.Complaints c JOIN dbo.Users u ON u.UserId = c.UserId
                ORDER BY c.CreatedAt DESC");
            return Done(rows);
        }

        [HttpPut("complaints/{id}/status")]
        public async Task<IActionResult> SetStatus(int id, [FromBody] StatusRequest req)
        {
            if (req.Status != "Resolved" && req.Status != "Pending")
                return BadRequest(new { Status = 400, Message = "Invalid status." });

            using var db = Db();

            // get the owner and the current status BEFORE updating
            var owner = await db.QueryFirstOrDefaultAsync<ComplaintOwner>(@"
                SELECT u.FullName, u.Email, c.Status
                FROM dbo.Complaints c JOIN dbo.Users u ON u.UserId = c.UserId
                WHERE c.ComplaintId = @id", new { id });

            if (owner == null)
                return NotFound(new { Status = 404, Message = "Complaint not found." });

            await db.ExecuteAsync(@"
                UPDATE dbo.Complaints
                SET Status = @s, ResolvedAt = CASE WHEN @s = 'Resolved' THEN GETDATE() ELSE NULL END
                WHERE ComplaintId = @id;
                INSERT INTO dbo.Notifications (UserId, ComplaintId, Message)
                SELECT UserId, ComplaintId, N'Your complaint was marked as ' + @s + N'.'
                FROM dbo.Complaints WHERE ComplaintId = @id;", new { s = req.Status, id });

            // email only when it changes to Resolved (no duplicate emails)
            if (req.Status == "Resolved" && owner.Status != "Resolved" && !string.IsNullOrWhiteSpace(owner.Email))
            {
                var name = System.Net.WebUtility.HtmlEncode(owner.FullName);
                var body = $@"
                    <div style='font-family:Arial,sans-serif;font-size:15px;color:#1a2232'>
                      <p>Hello {name},</p>
                      <p>Your complaint (No. {id}) has been marked as <b>Resolved</b> by the Barangay.</p>
                      <p>You can open the app to see the details. Thank you for reporting.</p>
                      <p>- Barangay Manguikay</p>
                    </div>";

                // SendAsync never throws, so the complaint stays resolved even if the email fails
                await _email.SendAsync(owner.Email, "Your complaint has been resolved", body);
            }

            return Done(null, "Status updated.");
        }

        // ---------- residents registry + pending verification ----------
        [HttpGet("residents")]
        public async Task<IActionResult> Residents()
        {
            using var db = Db();
            var residents = await db.QueryAsync(@"
                SELECT r.ResidentId, r.FullName, p.PurokName, r.Gender, r.BirthYear
                FROM dbo.Residents r LEFT JOIN dbo.Purok p ON p.PurokId = r.PurokId
                ORDER BY r.FullName");
            int withAccounts = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM dbo.Users WHERE VerificationStatus = 'Approved' AND IsDeleted = 0");
            return Done(new { total = residents.Count(), withAccounts, residents });
        }

        [HttpGet("pending")]
        public async Task<IActionResult> Pending()
        {
            using var db = Db();
            var rows = await db.QueryAsync(@"
                SELECT UserId, FullName, Email, IdType, CreatedAt FROM dbo.Users
                WHERE VerificationStatus = 'Pending' AND IsDeleted = 0 ORDER BY CreatedAt");
            return Done(rows);
        }

        [HttpPost("verify/{id}")]
        public async Task<IActionResult> Verify(int id, [FromBody] VerifyRequest req)
        {
            using var db = Db();
            await db.ExecuteAsync(@"UPDATE dbo.Users SET VerificationStatus = @s, IsActive = @a WHERE UserId = @id",
                new { s = req.Approve ? "Approved" : "Rejected", a = req.Approve, id });
            return Done(null, req.Approve ? "Account approved." : "Account rejected.");
        }

        // Private ID photos: only admins can stream them
        [HttpGet("id-image/{userId}/{type}")]
        public async Task<IActionResult> IdImage(int userId, string type)
        {
            string? col = type switch
            {
                "front" => "IdFrontImagePath",
                "back" => "IdBackImagePath",
                "selfie" => "LiveSelfieImagePath",
                _ => null
            };
            if (col == null) return BadRequest();

            using var db = Db();
            var rel = await db.QueryFirstOrDefaultAsync<string>($"SELECT {col} FROM dbo.Users WHERE UserId = @userId", new { userId });
            if (string.IsNullOrEmpty(rel)) return NotFound();

            var root = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "PrivateUploads"));
            var full = Path.GetFullPath(Path.Combine(root, rel));
            if (!full.StartsWith(root + Path.DirectorySeparatorChar) || !System.IO.File.Exists(full)) return NotFound();

            new FileExtensionContentTypeProvider().TryGetContentType(full, out var ct);
            return PhysicalFile(full, ct ?? "application/octet-stream");
        }

        // ---------- users: list / ban / unban / delete / restore ----------
        [HttpGet("users")]
        public async Task<IActionResult> Users()
        {
            using var db = Db();
            var rows = await db.QueryAsync(@"
                SELECT UserId, FullName, Email, ContactNumber, Age, Gender, ProfilePicturePath,
                       LastLoginAt, BanReason, BanUntil, IsDeleted,
                       CAST(CASE WHEN IsBanned = 1 AND (BanUntil IS NULL OR BanUntil > GETDATE()) THEN 1 ELSE 0 END AS BIT) AS IsBanActive
                FROM dbo.Users WHERE VerificationStatus = 'Approved'
                ORDER BY IsDeleted, FullName");
            return Done(rows);
        }

        [HttpPost("users/{id}/ban")]
        public async Task<IActionResult> Ban(int id, [FromBody] BanRequest req)
        {
            using var db = Db();
            await db.ExecuteAsync(@"
                UPDATE dbo.Users SET IsBanned = 1,
                    BanUntil = CASE WHEN @days > 0 THEN DATEADD(DAY, @days, GETDATE()) ELSE NULL END,
                    BanReason = @reason
                WHERE UserId = @id", new { id, days = req.Days ?? 0, reason = string.IsNullOrWhiteSpace(req.Reason) ? null : req.Reason });
            return Done(null, "User banned.");
        }

        [HttpPost("users/{id}/unban")]
        public async Task<IActionResult> Unban(int id)
        {
            using var db = Db();
            await db.ExecuteAsync("UPDATE dbo.Users SET IsBanned = 0, BanUntil = NULL, BanReason = NULL WHERE UserId = @id", new { id });
            return Done(null, "User unbanned.");
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var db = Db();
            await db.ExecuteAsync("UPDATE dbo.Users SET IsDeleted = 1, DeletedAt = GETDATE() WHERE UserId = @id", new { id });
            return Done(null, "Account deleted.");
        }

        [HttpPost("users/{id}/restore")]
        public async Task<IActionResult> Restore(int id)
        {
            using var db = Db();
            await db.ExecuteAsync("UPDATE dbo.Users SET IsDeleted = 0, DeletedAt = NULL WHERE UserId = @id", new { id });
            return Done(null, "Account restored.");
        }
    }
}