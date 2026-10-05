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
        private const string SP = "SP_BARANGAYREPORTING_ANNOUNCEMENT";

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
                        savedPaths.Add(await _fileStorage.SaveAnnouncementPhotoAsync(photo, adminId));
                    }
                }

                var param = new DynamicParameters();
                param.Add("@Action", "CREATE");
                param.Add("@AdminId", adminId);
                param.Add("@Caption", string.IsNullOrWhiteSpace(caption) ? null : caption);
                param.Add("@PhotoPaths", savedPaths.Count > 0 ? string.Join("|", savedPaths) : null);
                param.Add("@NewAnnouncementId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await conn.ExecuteAsync(SP, param, commandType: CommandType.StoredProcedure);

                service.Status = 200;
                service.Data = new { AnnouncementId = param.Get<int>("@NewAnnouncementId"), Caption = caption, Photos = savedPaths };
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
                    SP, new { Action = "GET" }, commandType: CommandType.StoredProcedure);

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
                await conn.ExecuteAsync(SP,
                    new { Action = "UPDATE", AnnouncementId = announcementId, Caption = caption },
                    commandType: CommandType.StoredProcedure);

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
                await conn.ExecuteAsync(SP,
                    new { Action = "DELETE", AnnouncementId = announcementId },
                    commandType: CommandType.StoredProcedure);

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