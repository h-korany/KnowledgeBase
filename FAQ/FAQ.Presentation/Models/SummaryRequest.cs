namespace FAQ.Presentation.Models
{
    public class SummaryRequest
    {
        public string QuestionTitle { get; set; } = string.Empty;
        public string QuestionContent { get; set; } = string.Empty;
        public List<string> Answers { get; set; } = new List<string>();
    }
}
