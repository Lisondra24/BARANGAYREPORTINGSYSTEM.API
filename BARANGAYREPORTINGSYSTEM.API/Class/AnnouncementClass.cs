using Dapper;
using BarangayReportingAPI.Model.Response;
using BarangayReportingAPI.IRepository;
using BarangayReportingAPI.Model;
using BarangayReportingAPI.Services;
using System.Data;
using System.Data.SqlClient;

namespace BarangayReportingAPI.Class
{
    public class AnnouncementClass : IAnnouncementRepository
    {
        private readonly IConfiguration _configuration;
        private readonly SqlConnection conn;
        private readonly IFileStorageService _fileStorage;

        public AnnouncementClass(IConfiguration config, IFileStorageService fileStorage)
        {
            _configuration = config;
            conn = new SqlConnection(config["ConnectionStrings:BarangayReportingDb"]);
            _fileStorage = fileStorage;
        }

        public async Task<ServiceResponse<object>> CreateAnnouncement(int adminId, string? caption, List<IFormFile>? photos)
        {
            var service = new ServiceResponse<object>();
            try
            {
                if (string.IsNullOrWhiteSpace(caption) && (photos == null || photos.Count == 0))
                {
                    service.Status = 400;
                    service.Message = "An announcement needs a caption or at least one photo.";
                    return service;
                }

                var savedPaths = new List<string>();
                if (photos != null)
                {
                    foreach (var photo in photos)
                    {
                        if (photo.Length == 0) continue;
                        string path = await _fileStorage.SaveAnnouncementPhotoAsync(photo, adminId);
                        savedPaths.Add(path);
                    }
                }

                string? photoPathsCombined = savedPaths.Count > 0 ? string.Join("|", savedPaths) : null;

                var param = new DynamicParameters();
                param.Add("@AdminId", adminId);
                param.Add("@Caption", string.IsNullOrWhiteSpace(caption) ? null : caption);
                param.Add("@PhotoPaths", photoPathsCombined);
                param.Add("@NewAnnouncementId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await conn.ExecuteAsync("SP_BARANGAYREPORTING_CREATEANNOUNCEMENT", param, commandType: CommandType.StoredProcedure);

                int newId = param.Get<int>("@NewAnnouncementId");

                service.Status = 200;
                service.Data = new { AnnouncementId = newId, Caption = caption, Photos = savedPaths };
                service.Message = "Announcement posted.";
            }
            catch (Exception ex)
            {
                service.Status = 500;
                service.Message = ex.Message;
            }
            return service;
        }

        public async Task<ServiceResponse<object>> GetAnnouncements()
        {
            var service = new ServiceResponse<object>();
            try
            {
                var rows = await conn.QueryAsync<AnnouncementModel>(
                    "SP_BARANGAYREPORTING_GETANNOUNCEMENTS", commandType: CommandType.StoredProcedure);

                var result = rows.Select(r => new AnnouncementResponseModel
                {
                    AnnouncementId = r.AnnouncementId,
                    AdminId = r.AdminId,
                    AdminName = r.AdminName,
                    AdminProfilePicturePath = r.AdminProfilePicturePath,
                    Caption = r.Caption,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    Photos = string.IsNullOrEmpty(r.PhotoPaths)
                        ? new List<string>()
                        : r.PhotoPaths.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList()
                }).ToList();

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

        public async Task<ServiceResponse<object>> UpdateAnnouncement(int announcementId, string caption)
        {
            var service = new ServiceResponse<object>();
            try
            {
                var param = new DynamicParameters();
                param.Add("@AnnouncementId", announcementId);
                param.Add("@Caption", caption);

                await conn.ExecuteAsync("SP_BARANGAYREPORTING_UPDATEANNOUNCEMENT", param, commandType: CommandType.StoredProcedure);

                service.Status = 200;
                service.Message = "Announcement updated.";
            }
            catch (Exception ex)
            {
                service.Status = 500;
                service.Message = ex.Message;
            }
            return service;
        }

        public async Task<ServiceResponse<object>> DeleteAnnouncement(int announcementId)
        {
            var service = new ServiceResponse<object>();
            try
            {
                var param = new DynamicParameters();
                param.Add("@AnnouncementId", announcementId);

                await conn.ExecuteAsync("SP_BARANGAYREPORTING_DELETEANNOUNCEMENT", param, commandType: CommandType.StoredProcedure);

                service.Status = 200;
                service.Message = "Announcement deleted.";
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