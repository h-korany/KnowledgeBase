using FAQ.Application.DTOs;
using FAQ.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAQ.Application.Interfaces
{
    public interface IKnowledgeBaseService
    {
        Task<List<Question>> GetRelevantQuestionsAsync(string query);
        Task<KnowledgeBaseAnalysis> AnalyzeKnowledgeBaseAsync(string? category = null);
        Task<List<Question>> GetQuestionsByCategoryAsync(string category);
        Task<Dictionary<string, int>> GetCategoryStatisticsAsync();
        Task<List<Question>> GetUnansweredQuestionsAsync();
        Task<double> GetAnswerRateAsync();
        Task<List<string>> ExtractCategoriesFromQuestionsAsync();
    }
}
