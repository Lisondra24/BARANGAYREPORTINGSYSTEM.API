using Microsoft.AspNetCore.Mvc;
using BarangayReportingAPI.IRepository;
using BarangayReportingAPI.Model;
using BarangayReportingAPI.Services;
using BARANGAYREPORTINGSYSTEM.API.Services;   // NEW: for IEmailService

namespace BarangayReportingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegisterController : ControllerBase
    {
        private readonly IRegisterRepository _registerRepository;
        private readonly IFileStorageService _fileStorage;
        private readonly IEmailService _email;   // NEW

        public RegisterController(IRegisterRepository registerRepository,
                                  IFileStorageService fileStorage,
                                  IEmailService email)   // NEW
        {
            _registerRepository = registerRepository;
            _fileStorage = fileStorage;
            _email = email;   // NEW
        }

        [HttpGet("puroks")]
        public async Task<IActionResult> GetPuroks()
        {
            var result = await _registerRepository.GetPuroks();
            return StatusCode(result.Status, result);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Register([FromForm] RegisterRequestModel request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Status = 400, Message = "Invalid input." });

            string idFrontPath, idBackPath, liveSelfiePath;
            try
            {
                idFrontPath = await _fileStorage.SaveIdImageAsync(request.IdFrontImage, request.Username, "id_front");
                idBackPath = await _fileStorage.SaveIdImageAsync(request.IdBackImage, request.Username, "id_back");
                liveSelfiePath = await _fileStorage.SaveIdImageAsync(request.LiveSelfieImage, request.Username, "live_selfie");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Status = 400, Message = ex.Message });
            }

            var result = await _registerRepository.GetRegister(request, idFrontPath, idBackPath, liveSelfiePath);

            // NEW: send the confirmation email only if registration succeeded
            if (result.Status == 200)
            {
                var sent = await _email.SendAsync(
                    request.Email,
                    "Registration received - Barangay Manguikay",
                    EmailTemplates.RegistrationReceived(request.FullName));

                if (!sent)
                    result.Message += " (Confirmation email could not be sent.)";
            }

            return StatusCode(result.Status, result);
        }
    }
}