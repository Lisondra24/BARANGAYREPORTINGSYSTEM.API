using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BarangayReportingAPI.IRepository;
using BarangayReportingAPI.Model;
using BARANGAYREPORTINGSYSTEM.API.Models;

namespace BarangayReportingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotFaqController : ControllerBase
    {
        private readonly IChatbotFaqRepository _faqRepository;

        public ChatbotFaqController(IChatbotFaqRepository faqRepository)
        {
            _faqRepository = faqRepository;
        }

        // Mobile app (residents) calls this to fill the chatbot buttons.
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetFaqs()
        {
            var result = await _faqRepository.GetFaqs();
            return StatusCode(result.Status, result);
        }

        // Only the admin (PHP web panel) can add, edit, or delete.
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> PostFaq([FromBody] ChatbotFaqRequestModel request)
        {
            var result = await _faqRepository.CreateFaq(request.Question, request.Answer, request.SortOrder);
            return StatusCode(result.Status, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFaq(int id, [FromBody] ChatbotFaqRequestModel request)
        {
            var result = await _faqRepository.UpdateFaq(id, request.Question, request.Answer, request.SortOrder);
            return StatusCode(result.Status, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFaq(int id)
        {
            var result = await _faqRepository.DeleteFaq(id);
            return StatusCode(result.Status, result);
        }
    }
}