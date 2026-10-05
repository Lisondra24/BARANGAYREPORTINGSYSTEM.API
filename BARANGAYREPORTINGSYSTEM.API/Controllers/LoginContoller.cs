using Microsoft.AspNetCore.Mvc;
using BarangayReportingAPI.IRepository;
using BarangayReportingAPI.Model;

namespace BarangayReportingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ILoginRepository _loginRepository;

        public LoginController(ILoginRepository loginRepository)
        {
            _loginRepository = loginRepository;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequestModel request)
        {
            var result = await _loginRepository.GetLogin(request.EmailOrUsername, request.Password);
            return StatusCode(result.Status, result);
        }
    }
}