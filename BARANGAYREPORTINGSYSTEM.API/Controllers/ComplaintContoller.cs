using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BarangayReportingAPI.IRepository;
using BarangayReportingAPI.Model;
using System.IdentityModel.Tokens.Jwt;

namespace BarangayReportingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ComplaintController : ControllerBase
    {
        private readonly IComplaintRepository _complaintRepository;

        public ComplaintController(IComplaintRepository complaintRepository)
        {
            _complaintRepository = complaintRepository;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PostComplaint([FromForm] ComplaintRequestModel request)
        {
            var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { Status = 401, Message = "Invalid or missing token." });

            var result = await _complaintRepository.CreateComplaint(userId, request.ComplaintText, request.Photos);
            return StatusCode(result.Status, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetComplaints()
        {
            var result = await _complaintRepository.GetComplaints();
            return StatusCode(result.Status, result);
        }
    }
}