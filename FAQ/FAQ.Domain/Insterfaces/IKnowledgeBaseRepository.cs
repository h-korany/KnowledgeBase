using FAQ.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FAQ.Domain.Insterfaces;
public interface IKnowledgeBaseRepository
{
    Task<List<Question>> GetQuestionsAsync();
    Task<List<Question>> GetQuestionsWithAnswersAsync();
    Task<List<Question>> GetQuestionsBySearchTermsAsync(List<string> searchTerms);
    Task<List<Question>> GetQuestionsByCategoryAsync(string category);
    Task<List<Question>> GetUnansweredQuestionsAsync();
    Task<int> GetTotalQuestionsCountAsync();
    Task<int> GetAnsweredQuestionsCountAsync();
    Task<int> GetQuestionsCountByCategoryAsync(string category);
    Task<List<string>> GetAllQuestionTitlesAsync();
    Task<List<string>> GetAllQuestionContentsAsync();
    Task<int> GetQuestionsCountByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<int> GetAnswersCountByDateRangeAsync(DateTime startDate, DateTime endDate);
}