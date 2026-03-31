// Ben10Api/Controllers/QuizController.cs
using System.Text.Json;
using Ben10Api.Data;
using Ben10Api.Models;
using Ben10Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ben10Api.Controllers;

[ApiController]
[Route("api/quiz")]
public class QuizController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMatchingService _matching;
    private readonly IWebHostEnvironment _env;

    public QuizController(AppDbContext db, IMatchingService matching, IWebHostEnvironment env)
    {
        _db = db;
        _matching = matching;
        _env = env;
    }

    [HttpGet("questions")]
    public IActionResult GetQuestions()
    {
        var path = Path.Combine(_env.ContentRootPath, "Data", "questions.json");
        var json = System.IO.File.ReadAllText(path);
        var questions = JsonSerializer.Deserialize<List<QuizQuestion>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        // Strip trait data before sending to client
        var clientQuestions = questions!.Select(q => new
        {
            q.Id,
            q.Text,
            Answers = q.Answers.Select(a => new { a.Text })
        });
        return Ok(clientQuestions);
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitRequest request)
    {
        if (request.Answers == null || request.Answers.Count == 0)
            return BadRequest(new { error = "No answers provided." });

        var questionsPath = Path.Combine(_env.ContentRootPath, "Data", "questions.json");
        var aliensPath    = Path.Combine(_env.ContentRootPath, "Data", "aliens.json");
        var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var questions = JsonSerializer.Deserialize<List<QuizQuestion>>(
            System.IO.File.ReadAllText(questionsPath), jsonOpts)!;
        var aliens = JsonSerializer.Deserialize<List<AlienProfile>>(
            System.IO.File.ReadAllText(aliensPath), jsonOpts)!;

        var (match, traitScores) = _matching.Match(request.Answers, questions, aliens);

        var result = new QuizResult
        {
            SessionId        = request.SessionId,
            MatchedCharacter = match.Name,
            AnswersJson      = JsonSerializer.Serialize(request.Answers),
            TraitScoresJson  = JsonSerializer.Serialize(traitScores),
            CreatedAt        = DateTime.UtcNow
        };

        _db.QuizResults.Add(result);
        await _db.SaveChangesAsync();

        var topTraits = traitScores
            .OrderByDescending(kv => kv.Value)
            .Take(3)
            .Select(kv => kv.Key)
            .ToList();

        return Ok(new SubmitResponse
        {
            ResultId        = result.Id,
            MatchedCharacter = match.Name,
            Description     = match.Description,
            ImagePath       = match.Image,
            MatchedTraits   = topTraits
        });
    }
}
