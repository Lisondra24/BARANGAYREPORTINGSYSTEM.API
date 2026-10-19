using BARANGAYREPORTINGSYSTEM.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BarangayReportingAPI.IRepository;
using BarangayReportingAPI.Model;
using System.IdentityModel.Tokens.Jwt;

namespace BarangayReportingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnnouncementController : ControllerBase
    {
        private readonly IAnnouncementRepository _announcementRepository;

        public AnnouncementController(IAnnouncementRepository announcementRepository)
        {
            _announcementRepository = announcementRepository;
        }

        // Residents can view announcements too, just not create/edit/delete.
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAnnouncements()
        {
            var result = await _announcementRepository.GetAnnouncements();
            return StatusCode(result.Status, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PostAnnouncement([FromForm] AnnouncementRequestModel request)
        {
            var adminIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!int.TryParse(adminIdClaim, out int adminId))
                return Unauthorized(new { Status = 401, Message = "Invalid or missing token." });

            // Read every uploaded file directly, whatever name the client used
            var photos = Request.Form.Files.ToList();

            var result = await _announcementRepository.CreateAnnouncement(adminId, request.Caption, photos);
            return StatusCode(result.Status, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAnnouncement(int id, [FromBody] UpdateAnnouncementRequestModel request)
        {
            var result = await _announcementRepository.UpdateAnnouncement(id, request.Caption);
            return StatusCode(result.Status, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            var result = await _announcementRepository.DeleteAnnouncement(id);
            return StatusCode(result.Status, result);
        }
    }
}