namespace BarangayReportingAPI.Model
{
    public class AccountFlags { public bool IsBanned { get; set; } public DateTime? BanUntil { get; set; } public string? BanReason { get; set; } public bool IsDeleted { get; set; } }
    public class AdminRow { public int AdminId { get; set; } public string FullName { get; set; } = ""; public string Email { get; set; } = ""; public string Username { get; set; } = ""; public string PasswordHash { get; set; } = ""; public string? ProfilePicturePath { get; set; } }
    public class StatusRequest { public string Status { get; set; } = ""; }
    public class VerifyRequest { public bool Approve { get; set; } }
    public class BanRequest { public int? Days { get; set; } public string? Reason { get; set; } }
    public class UpdateProfileRequest { public string FullName { get; set; } = ""; public int? Age { get; set; } public string? Gender { get; set; } public string? Address { get; set; } public string? ContactNumber { get; set; } }
    public class ChangePasswordRequest { public string CurrentPassword { get; set; } = ""; public string NewPassword { get; set; } = ""; }
}