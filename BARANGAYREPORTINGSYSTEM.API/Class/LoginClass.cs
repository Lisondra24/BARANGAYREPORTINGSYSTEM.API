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
        // No heartbeat for this many minutes = the old session counts as dead
        private const int SessionTimeoutMinutes = 30;

        private readonly IConfiguration _configuration;
        private readonly SqlConnection conn;

        public LoginClass(IConfiguration config)
        {
            _configuration = config;
            conn = new SqlConnection(config["ConnectionStrings:BarangayReportingDb"]);
        }

        public async Task<ServiceResponse<object>> GetLogin(string emailOrUsername, string password,
                                                            string? deviceId = null, string? deviceName = null)
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

                // ---- one device per account ----
                string? sid = null;
                if (!string.IsNullOrWhiteSpace(deviceId))
                {
                    var existing = await conn.QueryFirstOrDefaultAsync<UserSessionRow>(@"
                        SELECT DeviceId, DeviceName,
                               CASE WHEN DATEDIFF(MINUTE, LastActiveAt, GETDATE()) < @Mins THEN 1 ELSE 0 END AS IsActive
                        FROM dbo.UserSessions WHERE UserId = @Id",
                        new { Id = result.UserId, Mins = SessionTimeoutMinutes });

                    if (existing != null && existing.IsActive && existing.DeviceId != deviceId)
                    {
                        service.Status = 409;
                        service.Message = $"This account is already logged in on \"{existing.DeviceName}\". Log out there first.";
                        return service;
                    }

                    sid = Guid.NewGuid().ToString("N");
                    await conn.ExecuteAsync(@"
                        MERGE dbo.UserSessions AS t
                        USING (SELECT @UserId AS UserId) AS s ON t.UserId = s.UserId
                        WHEN MATCHED THEN UPDATE SET DeviceId = @DeviceId, DeviceName = @DeviceName,
                                                     SessionToken = @Sid, CreatedAt = GETDATE(), LastActiveAt = GETDATE()
                        WHEN NOT MATCHED THEN INSERT (UserId, DeviceId, DeviceName, SessionToken)
                                              VALUES (@UserId, @DeviceId, @DeviceName, @Sid);",
                        new
                        {
                            UserId = result.UserId,
                            DeviceId = deviceId,
                            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? "Unknown device" : deviceName,
                            Sid = sid
                        });
                }

                await conn.ExecuteAsync("UPDATE dbo.Users SET LastLoginAt = GETDATE() WHERE UserId = @Id", new { Id = result.UserId });

                service.Status = 200;
                service.Data = new { result.UserId, result.FullName, result.PurokId, result.PurokName, result.Email, result.Username, result.VerificationStatus };
                service.Token = GenerateToken(result, sid);
                service.Message = "Login successful.";
            }
            catch (Exception ex)
            {
                service.Status = 500;
                service.Message = ex.Message;
            }
            return service;
        }

        public async Task<bool> Heartbeat(int userId, string sid)
        {
            var rows = await conn.ExecuteAsync(
                "UPDATE dbo.UserSessions SET LastActiveAt = GETDATE() WHERE UserId = @userId AND SessionToken = @sid",
                new { userId, sid });
            return rows > 0;   // false = this session was replaced or logged out
        }

        public async Task Logout(int userId, string sid)
        {
            await conn.ExecuteAsync(
                "DELETE FROM dbo.UserSessions WHERE UserId = @userId AND SessionToken = @sid",
                new { userId, sid });
        }

        private string GenerateToken(UserLoginModel user, string? sid = null)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"])
            );
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
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
            if (sid != null) claims.Add(new Claim("sid", sid));

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