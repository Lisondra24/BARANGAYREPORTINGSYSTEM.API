using BarangayReportingAPI.Model;
using BarangayReportingAPI.Model.Response;

namespace BarangayReportingAPI.IRepository
{
    public interface IRegisterRepository
    {
        Task<ServiceResponse<object>> GetPuroks();

        Task<ServiceResponse<object>> GetRegister(
            RegisterRequestModel model,
            string idFrontImagePath,
            string idBackImagePath,
            string liveSelfieImagePath);
    }
}