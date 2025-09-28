using FAQ.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAQ.Application.DTOs
{
    public class QuestionDto
    {
        public int? Id { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public ICollection<AnswerDto> Answers { get; set; } = new List<AnswerDto>();
    }
}
