using Dapper;
using BarangayReportingAPI.Model.Response;
using BarangayReportingAPI.IRepository;
using BarangayReportingAPI.Model;
using System.Data;
using System.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BarangayReportingAPI.Class
{
    public class LoginClass : ILoginRepository
    {
        private readonly IConfiguration _configuration;
        private readonly SqlConnection conn;

        public LoginClass(IConfiguration config)
        {
            _configuration = config;
            conn = new SqlConnection(config["ConnectionStrings:BarangayReportingDb"]);
        }

        public async Task<ServiceResponse<object>> GetLogin(string emailOrUsername, string password)
        {
            ServiceResponse<object> service = new ServiceResponse<object>();
            try
            {
                var param = new DynamicParameters();
                param.Add("@EmailOrUsername", emailOrUsername);

                var result = await conn.QueryFirstOrDefaultAsync<UserLoginModel>(
                    "SP_BARANGAYREPORTING_GETLOGIN", param, commandType: CommandType.StoredProcedure);

                if (result == null || !BCrypt.Net.BCrypt.Verify(password, result.PasswordHash))
                {
                    service.Status = 400;
                    service.Message = "Invalid Email/Username or Password";
                    return service;
                }

                if (result.VerificationStatus == "Pending")
                {
                    service.Status = 403;
                    service.Message = "Your account is still waiting for admin verification.";
                    return service;
                }
                if (result.VerificationStatus == "Rejected")
                {
                    service.Status = 403;
                    service.Message = "Your registration was rejected. Please contact the barangay office.";
                    return service;
                }

                var f = await conn.QueryFirstAsync<AccountFlags>(
                    "SELECT IsBanned, BanUntil, BanReason, IsDeleted FROM dbo.Users WHERE UserId = @Id",
                    new { Id = result.UserId });

                if (f.IsDeleted)
                {
                    service.Status = 403;
                    service.Message = "This account has been deleted.";
                    return service;
                }
                if (f.IsBanned && (f.BanUntil == null || f.BanUntil > DateTime.Now))
                {
                    service.Status = 403;
                    service.Message = (f.BanUntil == null ? "Your account has been banned." : $"Your account is banned until {f.BanUntil:MMM d, yyyy}.")
                                      + (string.IsNullOrWhiteSpace(f.BanReason) ? "" : $" Reason: {f.BanReason}");
                    return service;
                }

                await conn.ExecuteAsync("UPDATE dbo.Users SET LastLoginAt = GETDATE() WHERE UserId = @Id", new { Id = result.UserId });

                service.Status = 200;
                service.Data = new { result.UserId, result.FullName, result.PurokId, result.PurokName, result.Email, result.Username, result.VerificationStatus };
                service.Token = GenerateToken(result);
                service.Message = "Login successful.";
            }
            catch (Exception ex)
            {
                service.Status = 500;
                service.Message = ex.Message;
            }
            return service;
        }

        private string GenerateToken(UserLoginModel user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"])
            );
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("username", user.Username),
                new Claim("fullName", user.FullName),
                new Claim("purokId", user.PurokId.ToString()),
                new Claim("verificationStatus", user.VerificationStatus),
                new Claim("role", "User")
            };
            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(
                    Convert.ToDouble(jwtSettings["ExpiryMinutes"])
                ),
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}