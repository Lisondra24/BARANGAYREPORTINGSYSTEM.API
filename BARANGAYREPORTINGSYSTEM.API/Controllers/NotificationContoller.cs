using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;

namespace BarangayReportingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "User")]
    public class NotificationController : ControllerBase
    {
        private readonly IConfiguration _cfg;
        public NotificationController(IConfiguration cfg) { _cfg = cfg; }
        private SqlConnection Db() => new SqlConnection(_cfg["ConnectionStrings:BarangayReportingDb"]);
        private int Uid => int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using var db = Db();
            var rows = await db.QueryAsync(@"SELECT TOP 30 NotificationId, ComplaintId, Message, IsRead, CreatedAt
                                             FROM dbo.Notifications WHERE UserId = @u ORDER BY CreatedAt DESC", new { u = Uid });
            return Ok(new { Status = 200, Data = rows });
        }

        [HttpPost("read/{id}")]
        public async Task<IActionResult> Read(int id)
        {
            using var db = Db();
            await db.ExecuteAsync("UPDATE dbo.Notifications SET IsRead = 1 WHERE NotificationId = @id AND UserId = @u", new { id, u = Uid });
            return Ok(new { Status = 200 });
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> ReadAll()
        {
            using var db = Db();
            await db.ExecuteAsync("UPDATE dbo.Notifications SET IsRead = 1 WHERE UserId = @u", new { u = Uid });
            return Ok(new { Status = 200 });
        }
    }
}