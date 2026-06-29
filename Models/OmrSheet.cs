namespace OmrSheet.Models
{
    public class OMRSheet
    {
        public int Id { get; set; }
        public string StudentId { get; set; }
        public string OMRId { get; set; }
        public string TemplateId { get; set; } = "Template1";
        public int MarksObtained { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadedAt { get; set; }
        public string AnswersJson { get; set; }

        // Soft-delete flag: when true, the result is hidden from the dashboard
        // but kept in the database (recoverable).
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }
    }
}