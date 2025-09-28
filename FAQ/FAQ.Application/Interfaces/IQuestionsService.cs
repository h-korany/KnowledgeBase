using FAQ.Application.DTOs;
using FAQ.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAQ.Application.Interfaces
{
    public interface IQuestionsService
    {
        Task<IEnumerable<QuestionDto>> GetAllAsync();

        Task<QuestionDto?> GetByIdAsync(int id);

        Task<Question> AddAsync(QuestionDto dto, Guid userId);

        Task<Answer> AddAnswerAsync(AnswerDto dto, int questionId, Guid userId);

        //Task<bool> UpdateAsync(int id, UpdateQuestionDto dto)


        Task<bool> DeleteAsync(int id);
    }
}
