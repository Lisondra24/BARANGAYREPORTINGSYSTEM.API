using System.ComponentModel.DataAnnotations;

namespace BarangayReportingAPI.Model
{
    public class AnnouncementRequestModel
    {
        public string? Caption { get; set; }
        public List<IFormFile>? Photos { get; set; }
    }

    public class UpdateAnnouncementRequestModel
    {
        [Required]
        public string Caption { get; set; } = string.Empty;
    }

    public class AnnouncementModel
    {
        public int AnnouncementId { get; set; }
        public int AdminId { get; set; }
        public string AdminName { get; set; } = string.Empty;
        public string? AdminProfilePicturePath { get; set; }
        public string? Caption { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string PhotoPaths { get; set; } = string.Empty;
    }

    public class AnnouncementResponseModel
    {
        public int AnnouncementId { get; set; }
        public int AdminId { get; set; }
        public string AdminName { get; set; } = string.Empty;
        public string? AdminProfilePicturePath { get; set; }
        public string? Caption { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<string> Photos { get; set; } = new();
    }
}