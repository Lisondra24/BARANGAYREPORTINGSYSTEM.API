namespace BarangayReportingAPI.Model
{
    public class ComplaintRequestModel
    {
        public string? ComplaintText { get; set; }
        public List<IFormFile>? Photos { get; set; }
    }

    public class ComplaintModel
    {
        public int ComplaintId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePicturePath { get; set; }
        public string? ComplaintText { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string PhotoPaths { get; set; } = string.Empty;
    }

    public class ComplaintResponseModel
    {
        public int ComplaintId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePicturePath { get; set; }
        public string? ComplaintText { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<string> Photos { get; set; } = new();
 