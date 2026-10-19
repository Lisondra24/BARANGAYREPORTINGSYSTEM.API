using Dapper;
using BarangayReportingAPI.Model.Response;
using BarangayReportingAPI.IRepository;
using BarangayReportingAPI.Model;
using System.Data;
using System.Data.SqlClient;
using BARANGAYREPORTINGSYSTEM.API.Models;

namespace BarangayReportingAPI.Class
{
    public class ChatbotFaqClass : IChatbotFaqRepository
    {
        private const string SP = "SP_BARANGAYREPORTING_CHATBOTFAQ";

        private readonly SqlConnection conn;

        public ChatbotFaqClass(IConfiguration config)
        {
            conn = new SqlConnection(config["ConnectionStrings:BarangayReportingDb"]);
        }

        // Returns an error message, or null if the input is fine
        private static string? Validate(string? question, string? answer)
        {
            if (string.IsNullOrWhiteSpace(question) || string.IsNullOrWhiteSpace(answer))
                return "Both a question and an answer are required.";
            if (question.Trim().Length > 100)
                return "The question must be 100 characters or less.";
            if (answer.Trim().Length > 1000)
                return "The answer must be 1000 characters or less.";
            return null;
        }

        // Residents (mobile) and admin both use this
        public async Task<ServiceResponse<object>> GetFaqs()
        {
            var service = new ServiceResponse<object>();
            try
            {
                var rows = await conn.QueryAsync<ChatbotFaqModel>(
                    SP, new { Action = "GET" }, commandType: CommandType.StoredProcedure);

                service.Status = 200;
                service.Data = rows.ToList();
                service.Message = "Success";
            }
            catch (Exception ex)
            {
                service.Status = 500;
                service.Message = ex.Message;
            }
            return service;
        }

        public async Task<ServiceResponse<object>> CreateFaq(string question, string answer, int? sortOrder)
        {
            var service = new ServiceResponse<object>();
            try
            {
                var error = Validate(question, answer);
                if (error != null)
                {
                    service.Status = 400;
                    service.Message = error;
                    return service;
                }

                var param = new DynamicParameters();
                param.Add("@Action", "CREATE");
                param.Add("@Question", question.Trim());
                param.Add("@Answer", answer.Trim());
                param.Add("@SortOrder", sortOrder);
                param.Add("@NewFaqId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await conn.ExecuteAsync(SP, param, commandType: CommandType.StoredProcedure);

                service.Status = 200;
                service.Data = new { FaqId = param.Get<int>("@NewFaqId"), Question = question.Trim(), Answer = answer.Trim() };
                service.Message = "Question added.";
            }
            catch (Exception ex)
            {
                service.Status = 500;
                service.Message = ex.Message;
            }
            return service;
        }

        public async Task<ServiceResponse<object>> UpdateFaq(int faqId, string question, string answer, int? sortOrder)
        {
            var service = new ServiceResponse<object>();
            try
            {
                var error = Validate(question, answer);
                if (error != null)
                {
                    service.Status = 400;
                    service.Message = error;
                    return service;
                }

                await conn.ExecuteAsync(SP,
                    new { Action = "UPDATE", FaqId = faqId, Question = question.Trim(), Answer = answer.Trim(), SortOrder = sortOrder },
                    commandType: CommandType.StoredProcedure);

                service.Status = 200;
                service.Message = "Question updated.";
            }
            catch (Exception ex)
            {
                service.Status = 500;
                service.Message = ex.Message;
            }
            return service;
        }

        public async Task<ServiceResponse<object>> DeleteFaq(int faqId)
        {
            var service = new ServiceResponse<object>();
            try
            {
                await conn.ExecuteAsync(SP,
                    new { Action = "DELETE", FaqId = faqId },
                    commandType: CommandType.StoredProcedure);

                service.Status = 200;
                service.Message = "Question deleted.";
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