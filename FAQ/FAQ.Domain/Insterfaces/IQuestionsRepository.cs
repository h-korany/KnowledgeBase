using FAQ.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAQ.Domain.Insterfaces
{
    public interface IQuestionsRepository
    {
        Task<Question> AddAsync(Question question);
        Task<Answer> AddAnswerAsync(Answer answer);

        Task DeleteAsync(int id);

        Task<IEnumerable<Question>> GetAllAsync();

        Task<Question?> GetByIdAsync(int id);

        Task UpdateAsync(Question question);
    }
}
