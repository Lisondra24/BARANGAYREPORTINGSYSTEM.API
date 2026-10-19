namespace BARANGAYREPORTINGSYSTEM.API.Models
{ 
    // One row from dbo.ChatbotFaqs
    public class ChatbotFaqModel
    {
        public int FaqId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // What the admin (PHP) sends when adding or editing a question
    public class ChatbotFaqRequestModel
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public int? SortOrder { get; set; }
    }
}
