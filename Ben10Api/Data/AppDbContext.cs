// Ben10Api/Data/AppDbContext.cs
using Ben10Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Ben10Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<QuizResult> QuizResults => Set<QuizResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuizResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MatchedCharacter).HasMaxLength(100).IsRequired();
            entity.Property(e => e.AnswersJson).IsRequired();
            entity.Property(e => e.TraitScoresJson).IsRequired();
        });
    }
}
