namespace BarangayReportingAPI.Model
{
    public class LoginRequestModel
    {
        public string EmailOrUsername { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserLoginModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int PurokId { get; set; }
        public string PurokName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string VerificationStatus { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}