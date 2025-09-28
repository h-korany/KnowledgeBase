using FAQ.Application.DTOs;
using FAQ.Application.Interfaces;
using FAQ.Domain.Entities;
using FAQ.Domain.Insterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAQ.Application.Services
{
    public class QuestionsService: IQuestionsService
    {
        private readonly IQuestionsRepository _questionsRepository;
        public QuestionsService(IQuestionsRepository questionsRepository) {
            _questionsRepository = questionsRepository;
        }
        public async Task<IEnumerable<QuestionDto>> GetAllAsync()
        {
            var questions = await _questionsRepository.GetAllAsync();
            return questions.Select(question => new QuestionDto
            {
                Id = question.Id,
                Title = question.Title,
                Content = question.Content,
                Answers = question.Answers.Select(x=>new AnswerDto() { Content=x.Content,Id=x.Id,QuestionId=x.QuestionId }).ToList()
            }).ToList();
        }

        public async Task<QuestionDto?> GetByIdAsync(int id)
        {
            var question = await _questionsRepository.GetByIdAsync(id);
            if (question != null) {
                return new QuestionDto()
                {
                    Id = question.Id,
                    Title = question.Title,
                    Content = question.Content,
                    Answers = question.Answers.Select(x => new AnswerDto() { Content = x.Content, Id = x.Id, QuestionId = x.QuestionId }).ToList()
                };
            }
            return null;
        }

        public async Task<Question> AddAsync(QuestionDto dto,Guid userId)
        {
            var question = new Question
            {
                Title = dto.Title,
                Content = dto.Content,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            var createdQuestion = await _questionsRepository.AddAsync(question);

            return createdQuestion;
        }
        public async Task<Answer> AddAnswerAsync(AnswerDto dto, int questionId, Guid userId)
        {
            var answer = new Answer
            {
                QuestionId = dto.QuestionId,
                Content = dto.Content,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            var createdQuestion = await _questionsRepository.AddAnswerAsync(answer);

            return createdQuestion;
        }
        //public async Task<bool> UpdateAsync(int id, UpdateQuestionDto dto)
        //{
        //    var question = await _questionsRepository.GetByIdAsync(id);
        //    if (question == null) return false;

        //    // Update scalar properties
        //    question.Title = dto.Title;
        //    question.Description = dto.Description ?? string.Empty;
        //    question.StatusId = dto.StatusId;
        //    question.DueDate = dto.DueDate;
        //    question.TaskOrder = dto.TaskOrder;
        //    question.UpdatedAt = DateTime.UtcNow;

        //    // --- UPDATE ASSIGNEES ---
        //    // Get current assignee IDs
        //    var currentAssigneeIds = question.Assignees.Select(a => a.AssigneeId).ToList();

        //    // Find assignees to remove (present in current but not in new)
        //    var assigneesToRemove = question.Assignees
        //        .Where(a => !dto.AssigneeIds.Contains(a.AssigneeId))
        //        .ToList();

        //    // Find assignees to add (present in new but not in current)
        //    var assigneesToAdd = dto.AssigneeIds
        //        .Except(currentAssigneeIds)
        //        .Select(assigneeId => new TaskAssignee { AssigneeId = assigneeId });

        //    // Perform updates
        //    foreach (var assignee in assigneesToRemove)
        //    {
        //        question.Assignees.Remove(assignee);
        //    }

        //    foreach (var assignee in assigneesToAdd)
        //    {
        //        question.Assignees.Add(assignee);
        //    }

        //    await _questionsRepository.UpdateAsync(question);

        //    // Publish event
        //    try
        //    {
        //        var questionEvent = new TaskEvent
        //        {
        //            EventType = "Updated",
        //            TaskId = question.Id,
        //            Title = question.Title,
        //            Description = question.Description,
        //            Status = question.StatusId,
        //            Assignee = string.Join(",", dto.AssigneeIds),
        //            Creator = question.CreatorId,
        //            DueDate = question.DueDate
        //        };
        //        await _eventPublisher.PublishTaskEventAsync(questionEvent);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"--> Could not send async message: {ex.Message}");
        //    }

        //    return true;
        //}

        public async Task<bool> DeleteAsync(int id)
        {
            var question = await _questionsRepository.GetByIdAsync(id);
            if (question == null) return false;

            await _questionsRepository.DeleteAsync(id);
            
            return true;
        }
    }
}
