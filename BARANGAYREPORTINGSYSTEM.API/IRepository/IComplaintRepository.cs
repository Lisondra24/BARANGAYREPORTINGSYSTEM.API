using BarangayReportingAPI.Model;
using BarangayReportingAPI.Model.Response;

namespace BarangayReportingAPI.IRepository
{
    public interface IComplaintRepository
    {
        Task<ServiceResponse<object>> CreateComplaint(int userId, string? complaintText, List<IFormFile>? photos);
        Task<ServiceResponse<object>> GetComplaints();
    }
}