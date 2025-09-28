namespace FAQ.Presentation.Models
{
    public class AIAssistantResponse
    {
        public string Response { get; set; } = string.Empty;
        public List<int> RelatedQuestionIds { get; set; } = new List<int>();
        public string Source { get; set; } = "Rule-Based";
    }
}
