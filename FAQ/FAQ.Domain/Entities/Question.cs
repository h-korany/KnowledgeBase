using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FAQ.Domain.Entities;

public partial class Question
{
    public int Id { get; set; }

    public string Title { get; set; }

    public string Content { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
    [NotMapped]
    // Category can be inferred from content or stored separately if needed
    public string? InferredCategory { get; set; }
}
