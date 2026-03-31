// Ben10Api/Services/IMatchingService.cs
using Ben10Api.Models;

namespace Ben10Api.Services;

public interface IMatchingService
{
    // answers: key = question id, value = chosen answer index (0-based)
    // Returns the matched AlienProfile and the computed trait scores
    (AlienProfile Match, Dictionary<string, int> TraitScores) Match(
        Dictionary<string, int> answers,
        IReadOnlyList<QuizQuestion> questions,
        IReadOnlyList<AlienProfile> aliens);
}
