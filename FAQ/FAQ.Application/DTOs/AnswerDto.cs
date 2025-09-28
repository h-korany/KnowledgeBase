using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAQ.Application.DTOs
{
    public class AnswerDto
    {
        public int? Id { get; set; }

        public int QuestionId { get; set; }

        public string Content { get; set; }
    }
}
