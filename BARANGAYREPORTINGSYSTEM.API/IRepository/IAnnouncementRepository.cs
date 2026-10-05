using BarangayReportingAPI.Model;
using BarangayReportingAPI.Model.Response;

namespace BarangayReportingAPI.IRepository
{
    public interface IAnnouncementRepository
    {
        Task<ServiceResponse<object>> CreateAnnouncement(int adminId, string? caption, List<IFormFile>? photos);
        Task<ServiceResponse<object>> GetAnnouncements();
        Task<ServiceResponse<object>> UpdateAnnouncement(int announcementId, string caption);
        Task<ServiceResponse<object>> DeleteAnnouncement(int announcementId);
    }
}