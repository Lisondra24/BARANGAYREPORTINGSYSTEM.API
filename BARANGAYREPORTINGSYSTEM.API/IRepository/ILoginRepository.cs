using BarangayReportingAPI.Model.Response;

namespace BarangayReportingAPI.IRepository
{
    public interface ILoginRepository
    {
        Task<ServiceResponse<object>> GetLogin(string emailOrUsername, string password,
                                               string? deviceId = null, string? deviceName = null);
        Task<bool> Heartbeat(int userId, string sid);
        Task Logout(int userId, string sid);
    }
}