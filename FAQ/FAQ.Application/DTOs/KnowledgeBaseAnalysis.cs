using FAQ.Application.Models;

namespace FAQ.Application.DTOs
{
    public class KnowledgeBaseAnalysis
    {
        public int TotalQuestions { get; set; }
        public int AnsweredQuestions { get; set; }
        public int UnansweredQuestions { get; set; }
        public double AnswerRate { get; set; }
        public double AverageAnswersPerQuestion { get; set; }
        public List<string> PopularCategories { get; set; } = new List<string>();
        public Dictionary<string, int> CategoryStats { get; set; } = new Dictionary<string, int>();
        public List<QuestionActivity> RecentActivity { get; set; } = new List<QuestionActivity>();
        public List<string> SuggestedCategories { get; set; } = new List<string>();
    }
}
