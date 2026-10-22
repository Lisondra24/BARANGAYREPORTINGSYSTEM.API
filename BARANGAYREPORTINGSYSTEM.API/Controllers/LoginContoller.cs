using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BarangayReportingAPI.IRepository;
using BarangayReportingAPI.Model;

namespace BarangayReportingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        // false = a login without device info still works and the one-device check is skipped.
        // Set to true once every phone runs the new app build that sends deviceId/deviceName.
        private const bool RequireDeviceId = false;

        private readonly ILoginRepository _loginRepository;

        public LoginController(ILoginRepository loginRepository)
        {
            _loginRepository = loginRepository;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequestModel request)
        {
            if (RequireDeviceId && string.IsNullOrWhiteSpace(request.DeviceId))
                return BadRequest(new { status = 400, message = "Device information is required." });

            var result = await _loginRepository.GetLogin(
                request.EmailOrUsername, request.Password, request.DeviceId, request.DeviceName);
            return StatusCode(result.Status, result);
        }

        [Authorize]
        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat()
        {
            if (!TryGetSession(out var userId, out var sid)) return Unauthorized();
            if (sid == "") return Ok();   // token issued before one-device login existed
            return await _loginRepository.Heartbeat(userId, sid) ? Ok() : Unauthorized();
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            if (!TryGetSession(out var userId, out var sid)) return Unauthorized();
            if (sid != "") await _loginRepository.Logout(userId, sid);
            return Ok();
        }

        private bool TryGetSession(out int userId, out string sid)
        {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            sid = User.FindFirst("sid")?.Value ?? User.FindFirst(ClaimTypes.Sid)?.Value ?? "";
            return int.TryParse(sub, out userId);
        }
    }
}