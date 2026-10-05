using Dapper;
using BarangayReportingAPI.Model.Response;
using BarangayReportingAPI.IRepository;
using BarangayReportingAPI.Model;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using BCrypt.Net;

namespace BarangayReportingAPI.Class
{
    public class RegisterClass : IRegisterRepository
    {
        private readonly IConfiguration _configuration;
        private readonly SqlConnection conn;

        public RegisterClass(IConfiguration config)
        {
            _configuration = config;
            conn = new SqlConnection(config["ConnectionStrings:BarangayReportingDb"]);
        }

        public async Task<ServiceResponse<object>> GetPuroks()
        {
            ServiceResponse<object> service = new ServiceResponse<object>();
            try
            {
                var result = conn.Query<PurokModel>(
                    "SP_BARANGAYREPORTING_GETPUROKS", commandType: CommandType.StoredProcedure);

                service.Status = 200;
                service.Data = result;
                service.Message = "Success";
            }
            catch (Exception ex)
            {
                service.Status = 500;
                service.Message = ex.Message;
            }
            return service;
        }

        public async Task<ServiceResponse<object>> GetRegister(
    RegisterRequestModel model,
    string idFrontImagePath,
    string idBackImagePath,
    string liveSelfieImagePath)
        {
            var service = new ServiceResponse<object>();

            try
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, workFactor: 12);

                var param = new DynamicParameters();
                param.Add("@FullName", model.FullName);
                param.Add("@PurokId", model.PurokId);
                param.Add("@Email", model.Email);
                param.Add("@Username", model.Username);
                param.Add("@PasswordHash", passwordHash);
                param.Add("@IdType", model.IdType);                       // NEW
                param.Add("@IdFrontImagePath", idFrontImagePath);
                param.Add("@IdBackImagePath", idBackImagePath);
                param.Add("@LiveSelfieImagePath", liveSelfieImagePath);
                param.Add("@NewUserId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                param.Add("@ResultCode", dbType: DbType.Int32, direction: ParameterDirection.Output);
                param.Add("@ResultMessage", dbType: DbType.String, direction: ParameterDirection.Output, size: 200);

                conn.Execute("SP_BARANGAYREPORTING_REGISTER", param, commandType: CommandType.StoredProcedure);

                int resultCode = param.Get<int>("@ResultCode");
                string resultMessage = param.Get<string>("@ResultMessage");

                if (resultCode != 0)
                {
                    // 1 = email used, 2 = username used, 3 = invalid ID type
                    service.Status = 400;
                    service.Message = resultMessage;
                }
                else
                {
                    int newUserId = param.Get<int>("@NewUserId");

                    service.Status = 200;
                    service.Data = new
                    {
                        UserId = newUserId,
                        model.FullName,
                        model.Email,
                        model.Username,
                        model.IdType,                                      // NEW
                        VerificationStatus = "Pending"
                    };
                    service.Message = resultMessage;
                }
            }
            catch (Exception ex)
            {
                service.Status = 500;
                service.Message = ex.Message;
            }

            return service;
        }
    }
}