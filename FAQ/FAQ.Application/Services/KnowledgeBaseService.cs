using FAQ.Application.DTOs;
using FAQ.Application.Interfaces;
using FAQ.Application.Models;
using FAQ.Domain.Entities;
using FAQ.Domain.Insterfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class KnowledgeBaseService : IKnowledgeBaseService
{
    private readonly IKnowledgeBaseRepository _repository;
    private readonly ILogger<KnowledgeBaseService> _logger;

    public KnowledgeBaseService(IKnowledgeBaseRepository repository, ILogger<KnowledgeBaseService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<Question>> GetRelevantQuestionsAsync(string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                var allQuestions = await _repository.GetQuestionsWithAnswersAsync();
                return allQuestions
                    .OrderByDescending(q => q.CreatedAt)
                    .Take(10)
                    .ToList();
            }

            var searchTerms = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            var relevantQuestions = await _repository.GetQuestionsBySearchTermsAsync(searchTerms);

            // Apply scoring and ordering
            var scoredQuestions = relevantQuestions
                .Select(q => new
                {
                    Question = q,
                    Score = searchTerms.Count(term => q.Title.ToLower().Contains(term)) * 3 +
                           searchTerms.Count(term => q.Content.ToLower().Contains(term)) * 2 +
                           (q.Answers.Count > 0 ? 1 : 0)
                })
                .OrderByDescending(x => x.Score)
                .Take(10)
                .Select(x => x.Question)
                .ToList();

            return scoredQuestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting relevant questions for query: {Query}", query);
            return new List<Question>();
        }
    }

    public async Task<KnowledgeBaseAnalysis> AnalyzeKnowledgeBaseAsync(string? category = null)
    {
        try
        {
            var analysis = new KnowledgeBaseAnalysis();
            List<Question> questions;

            if (!string.IsNullOrEmpty(category))
            {
                questions = await _repository.GetQuestionsByCategoryAsync(category);
            }
            else
            {
                questions = await _repository.GetQuestionsWithAnswersAsync();
            }

            // Basic statistics
            analysis.TotalQuestions = questions.Count;
            analysis.AnsweredQuestions = questions.Count(q => q.Answers.Any());
            analysis.UnansweredQuestions = analysis.TotalQuestions - analysis.AnsweredQuestions;
            analysis.AnswerRate = analysis.TotalQuestions > 0 ?
                (double)analysis.AnsweredQuestions / analysis.TotalQuestions * 100 : 0;
            analysis.AverageAnswersPerQuestion = analysis.AnsweredQuestions > 0 ?
                (double)questions.Sum(q => q.Answers.Count) / analysis.AnsweredQuestions : 0;

            // Extract categories from question content and titles
            analysis.PopularCategories = ExtractCategoriesFromQuestions(questions);
            analysis.CategoryStats = await GetCategoryStatisticsAsync();
            analysis.SuggestedCategories = await ExtractCategoriesFromQuestionsAsync();

            // Recent activity (last 30 days)
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            analysis.RecentActivity = await GetRecentActivityAsync(thirtyDaysAgo);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing knowledge base");
            return new KnowledgeBaseAnalysis();
        }
    }

    public async Task<List<Question>> GetQuestionsByCategoryAsync(string category)
    {
        try
        {
            return await _repository.GetQuestionsByCategoryAsync(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions by category: {Category}", category);
            return new List<Question>();
        }
    }

    public async Task<Dictionary<string, int>> GetCategoryStatisticsAsync()
    {
        try
        {
            var questions = await _repository.GetQuestionsAsync();
            var categories = ExtractCategoriesFromQuestions(questions);

            var stats = new Dictionary<string, int>();
            foreach (var category in categories.Take(10))
            {
                var count = await _repository.GetQuestionsCountByCategoryAsync(category);
                stats[category] = count;
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category statistics");
            return new Dictionary<string, int>();
        }
    }

    public async Task<List<Question>> GetUnansweredQuestionsAsync()
    {
        try
        {
            var unansweredQuestions = await _repository.GetUnansweredQuestionsAsync();
            return unansweredQuestions
                .OrderByDescending(q => q.CreatedAt)
                .Take(20)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unanswered questions");
            return new List<Question>();
        }
    }

    public async Task<double> GetAnswerRateAsync()
    {
        try
        {
            var totalQuestions = await _repository.GetTotalQuestionsCountAsync();
            if (totalQuestions == 0) return 0;

            var answeredQuestions = await _repository.GetAnsweredQuestionsCountAsync();
            return (double)answeredQuestions / totalQuestions * 100;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating answer rate");
            return 0;
        }
    }

    public async Task<List<string>> ExtractCategoriesFromQuestionsAsync()
    {
        try
        {
            var questions = await _repository.GetQuestionsAsync();
            return ExtractCategoriesFromQuestions(questions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting categories from questions");
            return new List<string>();
        }
    }

    private List<string> ExtractCategoriesFromQuestions(List<Question> questions)
    {
        var commonWords = new HashSet<string> {
            "the", "a", "an", "is", "are", "how", "what", "why", "when", "where",
            "to", "in", "on", "at", "for", "with", "by", "about", "like", "through",
            "and", "or", "but", "if", "because", "as", "until", "while", "of", "from",
            "up", "down", "in", "out", "on", "off", "over", "under", "again", "further",
            "then", "once", "here", "there", "when", "where", "why", "how", "all", "any",
            "both", "each", "few", "more", "most", "other", "some", "such", "no", "nor",
            "not", "only", "own", "same", "so", "than", "too", "very", "can", "will",
            "just", "should", "now", "i", "me", "my", "we", "our", "you", "your",
            "he", "him", "his", "she", "her", "it", "its", "they", "them", "their"
        };

        var words = new List<string>();

        foreach (var question in questions)
        {
            // Extract from title
            words.AddRange(question.Title.ToLower()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length > 3 && !commonWords.Contains(word)));

            // Extract from content (first 100 words)
            words.AddRange(question.Content.ToLower()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(100)
                .Where(word => word.Length > 3 && !commonWords.Contains(word)));
        }

        // Get most frequent words as categories
        var categories = words
            .GroupBy(word => word)
            .OrderByDescending(g => g.Count())
            .Take(15)
            .Select(g => CapitalizeFirstLetter(g.Key))
            .ToList();

        return categories;
    }

    private string CapitalizeFirstLetter(string word)
    {
        if (string.IsNullOrEmpty(word))
            return word;

        return char.ToUpper(word[0]) + word.Substring(1);
    }

    private async Task<List<QuestionActivity>> GetRecentActivityAsync(DateTime sinceDate)
    {
        var activity = new List<QuestionActivity>();

        // Group by week for the last 4 weeks
        for (int i = 3; i >= 0; i--)
        {
            var weekStart = DateTime.UtcNow.AddDays(-7 * (i + 1));
            var weekEnd = DateTime.UtcNow.AddDays(-7 * i);
            var weekLabel = $"{weekStart:MMM dd} - {weekEnd:MMM dd}";

            var weekQuestions = await _repository.GetQuestionsCountByDateRangeAsync(weekStart, weekEnd);
            var weekAnswers = await _repository.GetAnswersCountByDateRangeAsync(weekStart, weekEnd);

            activity.Add(new QuestionActivity
            {
                Period = weekLabel,
                QuestionsCount = weekQuestions,
                AnswersCount = weekAnswers
            });
        }

        return activity;
    }
}