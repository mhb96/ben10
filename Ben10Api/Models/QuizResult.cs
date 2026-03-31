// Ben10Api/Models/QuizResult.cs
namespace Ben10Api.Models;

public class QuizResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public string MatchedCharacter { get; set; } = string.Empty;
    public string AnswersJson { get; set; } = string.Empty;
    public string TraitScoresJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
