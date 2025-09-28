using System;
using System.Collections.Generic;
using FAQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FAQ.Infrastucture.Data;

public partial class FaqContext : DbContext
{
    public FaqContext()
    {
    }

    public FaqContext(DbContextOptions<FaqContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Answer> Answers { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Answer>(entity =>
        {
            entity.ToTable("Answer");

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Content).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Question).WithMany(p => p.Answers)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("FK_Answer_Question");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("Question");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Content).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
