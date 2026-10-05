using BarangayReportingAPI.Model.Response;

namespace BarangayReportingAPI.IRepository
{
    public interface ILoginRepository
    {
        Task<ServiceResponse<object>> GetLogin(string emailOrUsername, string password);
    }
}