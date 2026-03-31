// Ben10Api/Services/MatchingService.cs
using Ben10Api.Models;

namespace Ben10Api.Services;

public class MatchingService : IMatchingService
{
    public (AlienProfile Match, Dictionary<string, int> TraitScores) Match(
        Dictionary<string, int> answers,
        IReadOnlyList<QuizQuestion> questions,
        IReadOnlyList<AlienProfile> aliens)
    {
        // 1. Accumulate trait scores from answers
        var traitScores = new Dictionary<string, int>();
        foreach (var (questionId, answerIndex) in answers)
        {
            var question = questions.FirstOrDefault(q => q.Id == questionId);
            if (question is null || answerIndex < 0 || answerIndex >= question.Answers.Count)
                continue;

            var chosenAnswer = question.Answers[answerIndex];
            foreach (var (trait, weight) in chosenAnswer.Traits)
            {
                traitScores.TryGetValue(trait, out var existing);
                traitScores[trait] = existing + weight;
            }
        }

        // 2. Score each alien against accumulated trait scores
        AlienProfile bestMatch = aliens[0];
        int bestScore = int.MinValue;

        foreach (var alien in aliens)
        {
            int score = 0;
            foreach (var (trait, alienWeight) in alien.Traits)
            {
                if (traitScores.TryGetValue(trait, out var userScore))
                    score += userScore * alienWeight;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = alien;
            }
        }

        return (bestMatch, traitScores);
    }
}
