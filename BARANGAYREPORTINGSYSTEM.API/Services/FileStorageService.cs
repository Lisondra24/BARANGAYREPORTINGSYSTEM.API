namespace BarangayReportingAPI.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;

        public FileStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveIdImageAsync(IFormFile file, string username, string imageType)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException($"No file was uploaded for {imageType}.");

            // Saved OUTSIDE wwwroot — not publicly accessible via static file serving
            string uploadsFolder = Path.Combine(_env.ContentRootPath, "PrivateUploads", username);

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string fileExtension = Path.GetExtension(file.FileName);
            string fileName = $"{imageType}_{Guid.NewGuid()}{fileExtension}";
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Path.Combine(username, fileName).Replace("\\", "/");
        }

        public async Task<string> SaveComplaintPhotoAsync(IFormFile file, int userId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file was uploaded.");

            // Public — these photos are meant to be viewed in the feed
            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "complaints", userId.ToString());

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string fileExtension = Path.GetExtension(file.FileName);
            string fileName = $"{Guid.NewGuid()}{fileExtension}";
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Path.Combine("uploads", "complaints", userId.ToString(), fileName).Replace("\\", "/");
        }

        public async Task<string> SaveAnnouncementPhotoAsync(IFormFile file, int adminId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file was uploaded.");

            // Public — these photos are meant to be viewed in the feed
            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "announcements", adminId.ToString());

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string fileExtension = Path.GetExtension(file.FileName);
            string fileName = $"{Guid.NewGuid()}{fileExtension}";
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Path.Combine("uploads", "announcements", adminId.ToString(), fileName).Replace("\\", "/");
        }
    }
}