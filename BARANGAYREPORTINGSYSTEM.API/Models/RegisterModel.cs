using System.ComponentModel.DataAnnotations;

namespace BarangayReportingAPI.Model
{
    public class RegisterRequestModel
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public int PurokId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^[A-Za-z0-9_.]{4,30}$",
            ErrorMessage = "Username must be 4-30 characters: letters, numbers, underscore or dot.")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        public string Password { get; set; } = string.Empty;

        // Allowed: License, VotersId, NationalId, PostalId, Other
        [Required]
        [RegularExpression("^(License|VotersId|NationalId|PostalId|Other)$",
            ErrorMessage = "Invalid ID type.")]
        public string IdType { get; set; } = string.Empty;

        [Required]
        public IFormFile IdFrontImage { get; set; } = null!;

        [Required]
        public IFormFile IdBackImage { get; set; } = null!;

        [Required]
        public IFormFile LiveSelfieImage { get; set; } = null!;
    }

    public class PurokModel
    {
        public int PurokId { get; set; }
        public string PurokName { get; set; } = string.Empty;
    }
}