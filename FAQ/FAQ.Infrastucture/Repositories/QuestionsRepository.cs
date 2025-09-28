using FAQ.Domain.Entities;
using FAQ.Domain.Insterfaces;
using FAQ.Infrastucture.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FAQ.Infrastucture.Repositories
{
    public class QuestionsRepository : IQuestionsRepository
    {
        private readonly FaqContext _context;
        private readonly IMemoryCache _cache;

        public QuestionsRepository(FaqContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // HIGH PRIORITY: Cache individual questions for quick access
        public async Task<Question?> GetByIdAsync(int id)
        {
            var cacheKey = $"question_{id}";

            if (_cache.TryGetValue(cacheKey, out Question cachedQuestion))
            {
                return cachedQuestion;
            }

            var question = await _context.Questions
                .Where(x => x.Id == id)
                .Include(x => x.Answers)
                .FirstOrDefaultAsync();

            if (question != null)
            {
                // Cache individual questions for 30 minutes
                _cache.Set(cacheKey, question, TimeSpan.FromMinutes(30));
            }

            return question;
        }

        // HIGH PRIORITY: Cache the complete questions list
        public async Task<IEnumerable<Question>> GetAllAsync(/*string creatorId*/)
        {
            const string cacheKey = "questions_list_all";

            if (_cache.TryGetValue(cacheKey, out IEnumerable<Question> cachedQuestions))
            {
                return cachedQuestions;
            }

            var questions = await _context.Questions
                .Include(x => x.Answers)
                .ToListAsync();

            // Cache for 15 minutes
            _cache.Set(cacheKey, questions, TimeSpan.FromMinutes(15));
            return questions;
        }

        // WRITE OPERATIONS: Invalidate cache when data changes
        public async Task<Question> AddAsync(Question question)
        {
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            // Invalidate relevant cache entries
            InvalidateQuestionCaches();
            return question;
        }

        public async Task<Answer> AddAnswerAsync(Answer answer)
        {
            _context.Answers.Add(answer);
            await _context.SaveChangesAsync();

            // Invalidate cache for the specific question and lists
            InvalidateQuestionCaches(answer.QuestionId);
            return answer;
        }

        public async Task UpdateAsync(Question question)
        {
            _context.Entry(question).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // Invalidate cache for the updated question
            InvalidateQuestionCaches(question.Id);
        }

        public async Task DeleteAsync(int id)
        {
            var questionToDelete = await _context.Questions.FindAsync(id);
            if (questionToDelete != null)
            {
                _context.Questions.Remove(questionToDelete);
                await _context.SaveChangesAsync();

                // Invalidate cache for the deleted question
                InvalidateQuestionCaches(id);
            }
        }

        // Cache invalidation methods
        private void InvalidateQuestionCaches(int? questionId = null)
        {
            // Remove the main questions list cache
            _cache.Remove("questions_list_all");

            // Remove KnowledgeBaseRepository caches that might be affected
            _cache.Remove("questions_all");
            _cache.Remove("questions_with_answers");
            _cache.Remove("questions_unanswered");
            _cache.Remove("count_questions_total");
            _cache.Remove("count_questions_answered");

            // Remove specific question cache if ID provided
            if (questionId.HasValue)
            {
                _cache.Remove($"question_{questionId.Value}");
            }

            // Note: Search and category caches are left to expire naturally
            // since invalidating all pattern-based caches is complex
        }
    }
}