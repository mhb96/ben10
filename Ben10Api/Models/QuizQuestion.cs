// Ben10Api/Models/QuizQuestion.cs
namespace Ben10Api.Models;

public class QuizQuestion
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<QuizAnswer> Answers { get; set; } = new();
}

public class QuizAnswer
{
    public string Text { get; set; } = string.Empty;
    public Dictionary<string, int> Traits { get; set; } = new();
}
