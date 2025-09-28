using FAQ.Domain.Entities;
using FAQ.Domain.Insterfaces;
using FAQ.Infrastucture.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class KnowledgeBaseRepository : IKnowledgeBaseRepository
{
    private readonly FaqContext _context;
    private readonly ILogger<KnowledgeBaseRepository> _logger;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _defaultCacheOptions;

    public KnowledgeBaseRepository(FaqContext context, ILogger<KnowledgeBaseRepository> logger, IMemoryCache cache)
    {
        _context = context;
        _logger = logger;
        _cache = cache;

        // Default cache settings: 15 minutes sliding, 1 hour absolute
        _defaultCacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(15))
            .SetAbsoluteExpiration(TimeSpan.FromHours(1));
    }

    // HIGH PRIORITY CACHE: Frequently accessed, expensive queries
    public async Task<List<Question>> GetQuestionsAsync()
    {
        const string cacheKey = "questions_all";

        if (_cache.TryGetValue(cacheKey, out List<Question> cachedQuestions))
        {
            _logger.LogDebug("Returning cached questions");
            return cachedQuestions;
        }

        try
        {
            var questions = await _context.Questions.ToListAsync();
            _cache.Set(cacheKey, questions, _defaultCacheOptions);
            _logger.LogDebug("Cached questions data");
            return questions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions");
            return new List<Question>();
        }
    }

    // HIGH PRIORITY CACHE: Includes answers, expensive operation
    public async Task<List<Question>> GetQuestionsWithAnswersAsync()
    {
        const string cacheKey = "questions_with_answers";

        if (_cache.TryGetValue(cacheKey, out List<Question> cachedQuestions))
        {
            _logger.LogDebug("Returning cached questions with answers");
            return cachedQuestions;
        }

        try
        {
            var questions = await _context.Questions
                .Include(q => q.Answers)
                .ToListAsync();

            _cache.Set(cacheKey, questions, _defaultCacheOptions);
            _logger.LogDebug("Cached questions with answers data");
            return questions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions with answers");
            return new List<Question>();
        }
    }

    // MEDIUM PRIORITY CACHE: Search results can be cached for short periods
    public async Task<List<Question>> GetQuestionsBySearchTermsAsync(List<string> searchTerms)
    {
        var searchKey = string.Join("_", searchTerms.OrderBy(x => x));
        var cacheKey = $"questions_search_{searchKey}";

        if (_cache.TryGetValue(cacheKey, out List<Question> cachedQuestions))
        {
            _logger.LogDebug("Returning cached search results for: {SearchTerms}", searchKey);
            return cachedQuestions;
        }

        try
        {
            var questions = await _context.Questions
                .Include(q => q.Answers)
                .Where(q =>
                    searchTerms.Any(term => q.Title.ToLower().Contains(term)) ||
                    searchTerms.Any(term => q.Content.ToLower().Contains(term))
                )
                .ToListAsync();

            // Cache search results for shorter time (5 minutes) since they're more dynamic
            var searchCacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

            _cache.Set(cacheKey, questions, searchCacheOptions);
            _logger.LogDebug("Cached search results for: {SearchTerms}", searchKey);
            return questions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions by search terms");
            return new List<Question>();
        }
    }

    // MEDIUM PRIORITY CACHE: Category-based queries
    public async Task<List<Question>> GetQuestionsByCategoryAsync(string category)
    {
        var cacheKey = $"questions_category_{category.ToLower()}";

        if (_cache.TryGetValue(cacheKey, out List<Question> cachedQuestions))
        {
            _logger.LogDebug("Returning cached questions for category: {Category}", category);
            return cachedQuestions;
        }

        try
        {
            var questions = await _context.Questions
                .Include(q => q.Answers)
                .Where(q =>
                    q.Title.ToLower().Contains(category.ToLower()) ||
                    q.Content.ToLower().Contains(category.ToLower())
                )
                .ToListAsync();

            _cache.Set(cacheKey, questions, _defaultCacheOptions);
            _logger.LogDebug("Cached questions for category: {Category}", category);
            return questions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions by category: {Category}", category);
            return new List<Question>();
        }
    }

    // HIGH PRIORITY CACHE: Unanswered questions don't change frequently
    public async Task<List<Question>> GetUnansweredQuestionsAsync()
    {
        const string cacheKey = "questions_unanswered";

        if (_cache.TryGetValue(cacheKey, out List<Question> cachedQuestions))
        {
            _logger.LogDebug("Returning cached unanswered questions");
            return cachedQuestions;
        }

        try
        {
            var questions = await _context.Questions
                .Include(q => q.Answers)
                .Where(q => !q.Answers.Any())
                .ToListAsync();

            _cache.Set(cacheKey, questions, _defaultCacheOptions);
            _logger.LogDebug("Cached unanswered questions");
            return questions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unanswered questions");
            return new List<Question>();
        }
    }

    // LOW PRIORITY CACHE: Simple count operations
    public async Task<int> GetTotalQuestionsCountAsync()
    {
        const string cacheKey = "count_questions_total";

        if (_cache.TryGetValue(cacheKey, out int cachedCount))
        {
            _logger.LogDebug("Returning cached total questions count");
            return cachedCount;
        }

        try
        {
            var count = await _context.Questions.CountAsync();
            _cache.Set(cacheKey, count, _defaultCacheOptions);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total questions count");
            return 0;
        }
    }

    // LOW PRIORITY CACHE: Simple count operations
    public async Task<int> GetAnsweredQuestionsCountAsync()
    {
        const string cacheKey = "count_questions_answered";

        if (_cache.TryGetValue(cacheKey, out int cachedCount))
        {
            _logger.LogDebug("Returning cached answered questions count");
            return cachedCount;
        }

        try
        {
            var count = await _context.Questions.CountAsync(q => q.Answers.Any());
            _cache.Set(cacheKey, count, _defaultCacheOptions);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting answered questions count");
            return 0;
        }
    }

    // MEDIUM PRIORITY CACHE: Category statistics
    public async Task<int> GetQuestionsCountByCategoryAsync(string category)
    {
        var cacheKey = $"count_questions_category_{category.ToLower()}";

        if (_cache.TryGetValue(cacheKey, out int cachedCount))
        {
            _logger.LogDebug("Returning cached questions count for category: {Category}", category);
            return cachedCount;
        }

        try
        {
            var count = await _context.Questions
                .CountAsync(q =>
                    q.Title.ToLower().Contains(category.ToLower()) ||
                    q.Content.ToLower().Contains(category.ToLower())
                );
            _cache.Set(cacheKey, count, _defaultCacheOptions);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions count by category: {Category}", category);
            return 0;
        }
    }

    // LOW PRIORITY: Not frequently used, but can be cached
    public async Task<List<string>> GetAllQuestionTitlesAsync()
    {
        const string cacheKey = "question_titles_all";

        if (_cache.TryGetValue(cacheKey, out List<string> cachedTitles))
        {
            _logger.LogDebug("Returning cached question titles");
            return cachedTitles;
        }

        try
        {
            var titles = await _context.Questions
                .Select(q => q.Title)
                .ToListAsync();
            _cache.Set(cacheKey, titles, _defaultCacheOptions);
            return titles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all question titles");
            return new List<string>();
        }
    }

    // LOW PRIORITY: Not frequently used
    public async Task<List<string>> GetAllQuestionContentsAsync()
    {
        const string cacheKey = "question_contents_all";

        if (_cache.TryGetValue(cacheKey, out List<string> cachedContents))
        {
            _logger.LogDebug("Returning cached question contents");
            return cachedContents;
        }

        try
        {
            var contents = await _context.Questions
                .Select(q => q.Content)
                .ToListAsync();
            _cache.Set(cacheKey, contents, _defaultCacheOptions);
            return contents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all question contents");
            return new List<string>();
        }
    }

    // LOW PRIORITY: Date range queries are usually unique
    public async Task<int> GetQuestionsCountByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var cacheKey = $"count_questions_daterange_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out int cachedCount))
            return cachedCount;

        try
        {
            var count = await _context.Questions
                .Where(q => q.CreatedAt >= startDate && q.CreatedAt < endDate)
                .CountAsync();
            _cache.Set(cacheKey, count, TimeSpan.FromMinutes(10)); // Shorter cache for date ranges
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions count by date range");
            return 0;
        }
    }

    // LOW PRIORITY: Date range queries are usually unique
    public async Task<int> GetAnswersCountByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var cacheKey = $"count_answers_daterange_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out int cachedCount))
            return cachedCount;

        try
        {
            var count = await _context.Answers
                .Where(a => a.CreatedAt >= startDate && a.CreatedAt < endDate)
                .CountAsync();
            _cache.Set(cacheKey, count, TimeSpan.FromMinutes(10)); // Shorter cache for date ranges
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting answers count by date range");
            return 0;
        }
    }

    // Cache invalidation method (call this when data changes)
    public void InvalidateCache()
    {
        // Remove all cache entries related to questions
        var keysToRemove = new List<string>
        {
            "questions_all",
            "questions_with_answers",
            "questions_unanswered",
            "count_questions_total",
            "count_questions_answered",
            "question_titles_all",
            "question_contents_all"
        };

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }

        // Remove pattern-based cache entries (search and category)
        // Note: This is simplified - in production you might want a more sophisticated pattern removal
        _logger.LogInformation("Cache invalidated for question data");
    }
}