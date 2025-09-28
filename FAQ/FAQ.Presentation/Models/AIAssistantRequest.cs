namespace FAQ.Presentation.Models
{
    public class AIAssistantRequest
    {
        public string Query { get; set; } = string.Empty;
        public string? Category { get; set; }
    }
}
