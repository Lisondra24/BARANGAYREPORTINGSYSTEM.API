using Dapper;
using BarangayReportingAPI.Model.Response;
using BarangayReportingAPI.IRepository;
using BarangayReportingAPI.Model;
using BarangayReportingAPI.Services;
using System.Data;
using System.Data.SqlClient;

namespace BarangayReportingAPI.Class
{
    public class ComplaintClass : IComplaintRepository
    {
        private readonly IConfiguration _configuration;
        private readonly SqlConnection conn;
        private readonly IFileStorageService _fileStorage;

        public ComplaintClass(IConfiguration config, IFileStorageService fileStorage)
        {
            _configuration = config;
            conn = new SqlConnection(config["ConnectionStrings:BarangayReportingDb"]);
            _fileStorage = fileStorage;
        }

        public async Task<ServiceResponse<object>> CreateComplaint(int userId, string? complaintText, List<IFormFile>? photos)
        {
            var service = new ServiceResponse<object>();
            try
            {
                if (string.IsNullOrWhiteSpace(complaintText) && (photos == null || photos.Count == 0))
                {
                    service.Status = 400;
                    service.Message = "A post needs text or at least one photo.";
                    return service;
                }

                // Save all photos to disk first, collect their relative paths
                var savedPaths = new List<string>();
                if (photos != null)
                {
                    foreach (var photo in photos)
                    {
                        if (photo.Length == 0) continue;
                        string path = await _fileStorage.SaveComplaintPhotoAsync(photo, userId);
                        savedPaths.Add(path);
                    }
                }

                string? photoPathsCombined = savedPaths.Count > 0
                    ? string.Join("|", savedPaths)
                    : null;

                var param = new DynamicParameters();
                param.Add("@UserId", userId);
                param.Add("@ComplaintText", string.IsNullOrWhiteSpace(complaintText) ? null : complaintText);
                param.Add("@PhotoPaths", photoPathsCombined);
                param.Add("@NewComplaintId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await conn.ExecuteAsync("SP_BARANGAYREPORTING_CREATECOMPLAINT", param, commandType: CommandType.StoredProcedure);

                int newComplaintId = param.Get<int>("@NewComplaintId");

                service.Status = 200;
                service.Data = new
                {
                    ComplaintId = newComplaintId,
                    ComplaintText = complaintText,
                    Status = "Pending",
                    Photos = savedPaths
                };
                service.Message = "Posted successfully.";
            }
            catch (Exception ex)
            {
                service.Status = 500;
                service.Message = ex.Message;
            }
            return service;
        }

        public async Task<ServiceResponse<object>> GetComplaints()
        {
            var service = new ServiceResponse<object>();
            try
            {
                var rows = await conn.QueryAsync<ComplaintModel>(
                    "SP_BARANGAYREPORTING_GETCOMPLAINTS", commandType: CommandType.StoredProcedure);

                var result = rows.Select(r => new ComplaintResponseModel
                {
                    ComplaintId = r.ComplaintId,
                    UserId = r.UserId,
                    FullName = r.FullName,
                    ProfilePicturePath = r.ProfilePicturePath,
                    ComplaintText = r.ComplaintText,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
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
    }
}