namespace OmrSheet.Models
{
    public class ExamAnswerKey
    {
        public int Id { get; set; }
        public string TemplateId { get; set; } = "Template1";
        public string AnswersJson { get; set; } = string.Empty;
        public int CorrectMark { get; set; } = 4;
        public int IncorrectMark { get; set; } = -1;
        public int UnattemptedMark { get; set; } = 0;
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = "Teacher";
    }
}