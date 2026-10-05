using Microsoft.AspNetCore.Http;

namespace BarangayReportingAPI.Services
{
    public interface IFileStorageService
    {
        Task<string> SaveIdImageAsync(IFormFile file, string username, string imageType);
        Task<string> SaveComplaintPhotoAsync(IFormFile file, int userId);
        Task<string> SaveAnnouncementPhotoAsync(IFormFile file, int adminId);
    }
}