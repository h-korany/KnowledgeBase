using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FAQ.Domain.Entities;

public partial class Answer
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public string Content { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Question? Question { get; set; }
}
