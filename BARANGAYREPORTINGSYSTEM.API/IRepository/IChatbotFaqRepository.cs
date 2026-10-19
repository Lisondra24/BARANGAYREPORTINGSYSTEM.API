using BarangayReportingAPI.Model.Response;

namespace BarangayReportingAPI.IRepository
{
    public interface IChatbotFaqRepository
    {
        Task<ServiceResponse<object>> GetFaqs();
        Task<ServiceResponse<object>> CreateFaq(string question, string answer, int? sortOrder);
        Task<ServiceResponse<object>> UpdateFaq(int faqId, string question, string answer, int? sortOrder);
        Task<ServiceResponse<object>> DeleteFaq(int faqId);
    }
}