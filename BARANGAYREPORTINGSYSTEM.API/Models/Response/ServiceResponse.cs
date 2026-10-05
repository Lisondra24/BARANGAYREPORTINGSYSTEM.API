namespace BarangayReportingAPI.Model.Response
{
    public class ServiceResponse<T>
    {
        public int Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public string? Token { get; set; }
    }
}