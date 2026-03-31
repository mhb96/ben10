// Ben10Api.Tests/MatchingServiceTests.cs
using Ben10Api.Models;
using Ben10Api.Services;

namespace Ben10Api.Tests;

public class MatchingServiceTests
{
    private readonly IMatchingService _sut = new MatchingService();

    private static List<QuizQuestion> OneQuestion(string traitKey, int traitValue) =>
    [
        new QuizQuestion
        {
            Id = "q1",
            Text = "Test question",
            Answers =
            [
                new QuizAnswer { Text = "A", Traits = new Dictionary<string, int> { { traitKey, traitValue } } },
                new QuizAnswer { Text = "B", Traits = new Dictionary<string, int>() }
            ]
        }
    ];

    private static List<AlienProfile> TwoAliens() =>
    [
        new AlienProfile { Name = "Alpha", Image = "", Description = "", Traits = new Dictionary<string, int> { { "brave", 3 } } },
        new AlienProfile { Name = "Beta",  Image = "", Description = "", Traits = new Dictionary<string, int> { { "intelligent", 3 } } }
    ];

    [Fact]
    public void Match_SelectsAlienWithHighestTraitScore()
    {
        var questions = OneQuestion("brave", 2);
        var aliens = TwoAliens();
        var answers = new Dictionary<string, int> { { "q1", 0 } }; // picks "brave" answer

        var (match, _) = _sut.Match(answers, questions, aliens);

        Assert.Equal("Alpha", match.Name);
    }

    [Fact]
    public void Match_TieBreak_ReturnsFirstAlien()
    {
        // Both questions score equally for both aliens
        var questions = new List<QuizQuestion>
        {
            new() { Id = "q1", Text = "Q", Answers = [
                new QuizAnswer { Text = "A", Traits = new Dictionary<string, int> { { "brave", 3 }, { "intelligent", 3 } } }
            ]}
        };
        var aliens = TwoAliens();
        var answers = new Dictionary<string, int> { { "q1", 0 } };

        var (match, _) = _sut.Match(answers, questions, aliens);

        Assert.Equal("Alpha", match.Name); // first alien wins tie
    }

    [Fact]
    public void Match_EmptyAnswers_ReturnsFirstAlien()
    {
        var questions = OneQuestion("brave", 2);
        var aliens = TwoAliens();
        var answers = new Dictionary<string, int>(); // no answers

        var (match, _) = _sut.Match(answers, questions, aliens);

        Assert.Equal("Alpha", match.Name); // all scores 0, first alien wins
    }

    [Fact]
    public void Match_ReturnsCorrectTraitScores()
    {
        var questions = OneQuestion("brave", 2);
        var aliens = TwoAliens();
        var answers = new Dictionary<string, int> { { "q1", 0 } };

        var (_, traitScores) = _sut.Match(answers, questions, aliens);

        Assert.Equal(2, traitScores["brave"]);
    }
}
