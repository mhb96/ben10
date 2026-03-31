# Ben 10 Personality Matcher — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a responsive web app that quizzes users on their personality and matches them to a Ben 10 original-series alien, with results saved to MSSQL and a downloadable shareable card.

**Architecture:** React (Vite) SPA frontend communicates with an ASP.NET Core Web API backend via REST/JSON. Matching logic lives entirely in the backend. Results are persisted to a MSSQL `QuizResults` table. Shareable cards are generated client-side with `html2canvas`.

**Tech Stack:** React 18, Vite, html2canvas, ASP.NET Core 8 Web API, Entity Framework Core, Microsoft SQL Server, xUnit.

---

## File Structure

### Backend (`Ben10Api/`)
```
Ben10Api/
  Ben10Api.csproj
  Program.cs                          # app bootstrap, DI, EF, CORS
  appsettings.json                    # connection string placeholder
  Data/
    AppDbContext.cs                   # EF DbContext with QuizResults
    questions.json                    # quiz questions + trait mappings
    aliens.json                       # alien profiles + trait weights
  Models/
    QuizResult.cs                     # EF entity
    SubmitRequest.cs                  # POST /api/quiz/submit request body
    SubmitResponse.cs                 # POST /api/quiz/submit response body
    QuizQuestion.cs                   # deserialized question shape
    AlienProfile.cs                   # deserialized alien profile shape
  Services/
    MatchingService.cs                # trait scoring + alien matching logic
    IMatchingService.cs               # interface for DI
  Controllers/
    QuizController.cs                 # GET /api/quiz/questions, POST /api/quiz/submit
    ResultsController.cs              # GET /api/results/{id}
Ben10Api.Tests/
  Ben10Api.Tests.csproj
  MatchingServiceTests.cs             # unit tests for matching algorithm
  QuizControllerIntegrationTests.cs  # integration tests for API endpoints
```

### Frontend (`ben10-frontend/`)
```
ben10-frontend/
  index.html
  vite.config.js
  package.json
  public/
    images/
      aliens/                         # heatblast.png, xlr8.png, etc. (add manually)
  src/
    main.jsx
    App.jsx                           # routing (react-router-dom)
    api/
      quizApi.js                      # fetch wrappers for all 3 endpoints
    data/
      (none — questions fetched from API)
    pages/
      Home.jsx                        # intro + start button
      Quiz.jsx                        # question-by-question, progress bar
      Loading.jsx                     # spinner + error state
      Result.jsx                      # alien result, traits, card, retake
    components/
      ProgressBar.jsx                 # progress bar used in Quiz
      ResultCard.jsx                  # the shareable card DOM element
    styles/
      index.css                       # global styles, responsive layout
```

---

## Task 1: Backend Project Scaffold

**Files:**
- Create: `Ben10Api/Ben10Api.csproj`
- Create: `Ben10Api/Program.cs`
- Create: `Ben10Api/appsettings.json`

- [ ] **Step 1: Create the ASP.NET Core Web API project**

```bash
dotnet new webapi -n Ben10Api --no-openapi
cd Ben10Api
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
```

- [ ] **Step 2: Replace `Program.cs` with minimal bootstrap**

```csharp
// Ben10Api/Program.cs
using Ben10Api.Data;
using Ben10Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IMatchingService, MatchingService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();
app.UseCors();
app.MapControllers();
app.Run();
```

- [ ] **Step 3: Configure `appsettings.json`**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Ben10Db;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

- [ ] **Step 4: Verify project builds**

```bash
dotnet build
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add Ben10Api/
git commit -m "feat: scaffold ASP.NET Core Web API project"
```

---

## Task 2: Models

**Files:**
- Create: `Ben10Api/Models/QuizResult.cs`
- Create: `Ben10Api/Models/SubmitRequest.cs`
- Create: `Ben10Api/Models/SubmitResponse.cs`
- Create: `Ben10Api/Models/QuizQuestion.cs`
- Create: `Ben10Api/Models/AlienProfile.cs`

- [ ] **Step 1: Create `QuizResult.cs` (EF entity)**

```csharp
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
```

- [ ] **Step 2: Create `SubmitRequest.cs`**

```csharp
// Ben10Api/Models/SubmitRequest.cs
namespace Ben10Api.Models;

public class SubmitRequest
{
    public Guid SessionId { get; set; }
    // Key = question id (string), Value = answer index (int, 0-based)
    public Dictionary<string, int> Answers { get; set; } = new();
}
```

- [ ] **Step 3: Create `SubmitResponse.cs`**

```csharp
// Ben10Api/Models/SubmitResponse.cs
namespace Ben10Api.Models;

public class SubmitResponse
{
    public Guid ResultId { get; set; }
    public string MatchedCharacter { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public List<string> MatchedTraits { get; set; } = new();
}
```

- [ ] **Step 4: Create `QuizQuestion.cs`**

```csharp
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
```

- [ ] **Step 5: Create `AlienProfile.cs`**

```csharp
// Ben10Api/Models/AlienProfile.cs
namespace Ben10Api.Models;

public class AlienProfile
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public Dictionary<string, int> Traits { get; set; } = new();
}
```

- [ ] **Step 6: Verify build**

```bash
dotnet build Ben10Api/
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7: Commit**

```bash
git add Ben10Api/Models/
git commit -m "feat: add domain models"
```

---

## Task 3: Data Files (Questions + Alien Profiles)

**Files:**
- Create: `Ben10Api/Data/questions.json`
- Create: `Ben10Api/Data/aliens.json`

- [ ] **Step 1: Create `questions.json`**

```json
[
  {
    "id": "q1",
    "text": "When faced with a problem, you...",
    "answers": [
      { "text": "Charge straight in without thinking", "traits": { "impulsive": 2, "brave": 1 } },
      { "text": "Analyse it carefully first", "traits": { "intelligent": 2, "strategic": 1 } },
      { "text": "Find a creative workaround", "traits": { "creative": 2, "adaptable": 1 } },
      { "text": "Get others involved", "traits": { "empathetic": 2, "teamwork": 1 } }
    ]
  },
  {
    "id": "q2",
    "text": "Your friends would describe you as...",
    "answers": [
      { "text": "Hot-headed but loyal", "traits": { "hot-headed": 2, "loyal": 2 } },
      { "text": "Quiet but observant", "traits": { "intelligent": 2, "strategic": 1 } },
      { "text": "Wild and energetic", "traits": { "energetic": 2, "instinctive": 1 } },
      { "text": "Tough and dependable", "traits": { "strong": 2, "brave": 1 } }
    ]
  },
  {
    "id": "q3",
    "text": "In a group project, you tend to...",
    "answers": [
      { "text": "Take charge and lead", "traits": { "brave": 2, "impulsive": 1 } },
      { "text": "Plan and organise everything", "traits": { "strategic": 2, "intelligent": 1 } },
      { "text": "Do the heavy lifting", "traits": { "strong": 2, "loyal": 1 } },
      { "text": "Keep the mood light and creative", "traits": { "creative": 2, "adaptable": 1 } }
    ]
  },
  {
    "id": "q4",
    "text": "Your biggest strength is your...",
    "answers": [
      { "text": "Raw power", "traits": { "strong": 3 } },
      { "text": "Speed and quick thinking", "traits": { "energetic": 2, "strategic": 1 } },
      { "text": "Intelligence and logic", "traits": { "intelligent": 3 } },
      { "text": "Adaptability", "traits": { "adaptable": 2, "creative": 1 } }
    ]
  },
  {
    "id": "q5",
    "text": "How do you handle fear?",
    "answers": [
      { "text": "Push through it — fear means nothing", "traits": { "brave": 3 } },
      { "text": "Figure out what's causing it", "traits": { "intelligent": 2, "strategic": 1 } },
      { "text": "Use instinct to survive", "traits": { "instinctive": 2, "adaptable": 1 } },
      { "text": "It gets under your skin — you're sensitive to it", "traits": { "mysterious": 2, "impulsive": 1 } }
    ]
  },
  {
    "id": "q6",
    "text": "Which environment feels most natural to you?",
    "answers": [
      { "text": "Anywhere hot — you thrive in heat", "traits": { "hot-headed": 2, "passionate": 1 } },
      { "text": "Water — calm, deep, flowing", "traits": { "adaptable": 2, "instinctive": 1 } },
      { "text": "Open fields — room to run", "traits": { "energetic": 2, "instinctive": 1 } },
      { "text": "Labs or libraries — anywhere with information", "traits": { "intelligent": 3 } }
    ]
  },
  {
    "id": "q7",
    "text": "When technology breaks, you...",
    "answers": [
      { "text": "Merge with it and fix it yourself", "traits": { "adaptable": 2, "intelligent": 1 } },
      { "text": "Smash it out of frustration", "traits": { "impulsive": 2, "hot-headed": 1 } },
      { "text": "Methodically troubleshoot", "traits": { "strategic": 2, "intelligent": 1 } },
      { "text": "Ignore it and rely on instinct", "traits": { "instinctive": 2 } }
    ]
  },
  {
    "id": "q8",
    "text": "Which word resonates with you most?",
    "answers": [
      { "text": "Power", "traits": { "strong": 2, "brave": 1 } },
      { "text": "Stealth", "traits": { "mysterious": 3 } },
      { "text": "Speed", "traits": { "energetic": 3 } },
      { "text": "Wisdom", "traits": { "intelligent": 3 } }
    ]
  },
  {
    "id": "q9",
    "text": "How do you recharge?",
    "answers": [
      { "text": "Physical activity — movement is life", "traits": { "energetic": 2, "instinctive": 1 } },
      { "text": "Reading or learning something new", "traits": { "intelligent": 2, "strategic": 1 } },
      { "text": "Being around people you trust", "traits": { "empathetic": 2, "loyal": 1 } },
      { "text": "Solitude — you prefer your own company", "traits": { "mysterious": 2, "strategic": 1 } }
    ]
  },
  {
    "id": "q10",
    "text": "Your ideal superpower is...",
    "answers": [
      { "text": "Incredible strength", "traits": { "strong": 3 } },
      { "text": "Flight and fire", "traits": { "brave": 2, "passionate": 1 } },
      { "text": "Phasing through walls invisibly", "traits": { "mysterious": 3 } },
      { "text": "Super speed", "traits": { "energetic": 3 } }
    ]
  }
]
```

- [ ] **Step 2: Create `aliens.json`**

```json
[
  {
    "name": "Heatblast",
    "image": "/images/aliens/heatblast.png",
    "description": "You're passionate, bold, and never back down from a challenge. You act on instinct and your fiery energy is impossible to ignore.",
    "traits": { "impulsive": 3, "brave": 2, "hot-headed": 3, "passionate": 2 }
  },
  {
    "name": "Wildmutt",
    "image": "/images/aliens/wildmutt.png",
    "description": "You rely on raw instinct over logic. You're fiercely loyal, highly perceptive, and thrive in chaotic situations.",
    "traits": { "instinctive": 3, "loyal": 2, "energetic": 2, "brave": 1 }
  },
  {
    "name": "Diamondhead",
    "image": "/images/aliens/diamondhead.png",
    "description": "You're sharp, resilient, and unbreakable under pressure. Once you commit to something, nothing can stop you.",
    "traits": { "strong": 2, "strategic": 2, "brave": 2, "loyal": 1 }
  },
  {
    "name": "XLR8",
    "image": "/images/aliens/xlr8.png",
    "description": "You live at full speed. Quick thinking and boundless energy define you — you're always ten steps ahead.",
    "traits": { "energetic": 3, "strategic": 2, "impulsive": 1, "adaptable": 1 }
  },
  {
    "name": "Greymatter",
    "image": "/images/aliens/greymatter.png",
    "description": "Brains over brawn, every time. You're a problem-solver who sees patterns others miss and thrives on knowledge.",
    "traits": { "intelligent": 3, "strategic": 3, "adaptable": 1 }
  },
  {
    "name": "Four Arms",
    "image": "/images/aliens/fourarms.png",
    "description": "You're the rock everyone leans on. Powerful, dependable, and willing to take a hit to protect the people you care about.",
    "traits": { "strong": 3, "brave": 2, "loyal": 2, "impulsive": 1 }
  },
  {
    "name": "Stinkfly",
    "image": "/images/aliens/stinkfly.png",
    "description": "You're unpredictable, creative, and love catching people off guard. Your unconventional style is your greatest weapon.",
    "traits": { "creative": 3, "adaptable": 2, "energetic": 1, "instinctive": 1 }
  },
  {
    "name": "Ripjaws",
    "image": "/images/aliens/ripjaws.png",
    "description": "You're calm on the surface but fierce when provoked. You adapt effortlessly to whatever situation you're in.",
    "traits": { "adaptable": 3, "instinctive": 2, "strategic": 1, "strong": 1 }
  },
  {
    "name": "Upgrade",
    "image": "/images/aliens/upgrade.png",
    "description": "You elevate everything around you. Tech-minded, fluid, and resourceful — you make systems better just by being part of them.",
    "traits": { "adaptable": 2, "intelligent": 2, "creative": 2, "strategic": 1 }
  },
  {
    "name": "Ghostfreak",
    "image": "/images/aliens/ghostfreak.png",
    "description": "Mysterious and introspective, you operate in the shadows. People rarely see you coming — and that's exactly how you like it.",
    "traits": { "mysterious": 3, "strategic": 2, "impulsive": 1 }
  }
]
```

- [ ] **Step 3: Mark both files as content in the `.csproj` so they are copied to output**

In `Ben10Api/Ben10Api.csproj`, add inside `<Project>`:

```xml
<ItemGroup>
  <Content Include="Data\questions.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  <Content Include="Data\aliens.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

- [ ] **Step 4: Commit**

```bash
git add Ben10Api/Data/
git commit -m "feat: add questions and alien profile data files"
```

---

## Task 4: Database Context + Migration

**Files:**
- Create: `Ben10Api/Data/AppDbContext.cs`

- [ ] **Step 1: Create `AppDbContext.cs`**

```csharp
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
```

- [ ] **Step 2: Create and apply the initial migration**

```bash
cd Ben10Api
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Expected: Migration files created in `Migrations/`. Database and `QuizResults` table created in SQL Server.

- [ ] **Step 3: Verify build**

```bash
dotnet build
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add Ben10Api/Data/AppDbContext.cs Ben10Api/Migrations/
git commit -m "feat: add EF DbContext and initial migration"
```

---

## Task 5: Matching Service (with tests)

**Files:**
- Create: `Ben10Api/Services/IMatchingService.cs`
- Create: `Ben10Api/Services/MatchingService.cs`
- Create: `Ben10Api.Tests/Ben10Api.Tests.csproj`
- Create: `Ben10Api.Tests/MatchingServiceTests.cs`

- [ ] **Step 1: Create the xUnit test project**

```bash
dotnet new xunit -n Ben10Api.Tests
cd Ben10Api.Tests
dotnet add reference ../Ben10Api/Ben10Api.csproj
```

- [ ] **Step 2: Create `IMatchingService.cs`**

```csharp
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
```

- [ ] **Step 3: Write failing tests in `MatchingServiceTests.cs`**

```csharp
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
```

- [ ] **Step 4: Run tests — verify they fail**

```bash
dotnet test Ben10Api.Tests/
```
Expected: Compile error — `MatchingService` not yet defined.

- [ ] **Step 5: Create `MatchingService.cs`**

```csharp
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
```

- [ ] **Step 6: Run tests — verify they pass**

```bash
dotnet test Ben10Api.Tests/
```
Expected: `Passed! - Failed: 0, Passed: 4`

- [ ] **Step 7: Commit**

```bash
git add Ben10Api/Services/ Ben10Api.Tests/
git commit -m "feat: add matching service with unit tests"
```

---

## Task 6: Quiz Controller

**Files:**
- Create: `Ben10Api/Controllers/QuizController.cs`

- [ ] **Step 1: Create `QuizController.cs`**

```csharp
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
```

- [ ] **Step 2: Verify build**

```bash
dotnet build Ben10Api/
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add Ben10Api/Controllers/QuizController.cs
git commit -m "feat: add QuizController (GET questions, POST submit)"
```

---

## Task 7: Results Controller

**Files:**
- Create: `Ben10Api/Controllers/ResultsController.cs`

- [ ] **Step 1: Create `ResultsController.cs`**

```csharp
// Ben10Api/Controllers/ResultsController.cs
using Ben10Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ben10Api.Controllers;

[ApiController]
[Route("api/results")]
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ResultsController(AppDbContext db) => _db = db;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetResult(Guid id)
    {
        var result = await _db.QuizResults.FindAsync(id);
        if (result is null)
            return NotFound(new { error = "Result not found." });

        return Ok(new
        {
            result.Id,
            result.MatchedCharacter,
            result.CreatedAt
        });
    }
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build Ben10Api/
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add Ben10Api/Controllers/ResultsController.cs
git commit -m "feat: add ResultsController (GET results by id)"
```

---

## Task 8: API Integration Tests

**Files:**
- Create: `Ben10Api.Tests/QuizControllerIntegrationTests.cs`

- [ ] **Step 1: Add test dependencies**

```bash
cd Ben10Api.Tests
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

- [ ] **Step 2: Make `Ben10Api` project support `WebApplicationFactory` — add to `Program.cs`**

At the very end of `Ben10Api/Program.cs`, add:

```csharp
// Required for WebApplicationFactory in tests
public partial class Program { }
```

- [ ] **Step 3: Write integration tests**

```csharp
// Ben10Api.Tests/QuizControllerIntegrationTests.cs
using System.Net;
using System.Net.Http.Json;
using Ben10Api.Data;
using Ben10Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ben10Api.Tests;

public class QuizControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public QuizControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace SQL Server with in-memory DB for tests
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetQuestions_Returns200AndQuestions()
    {
        var response = await _client.GetAsync("/api/quiz/questions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<object>>();
        Assert.NotNull(body);
        Assert.True(body.Count > 0);
    }

    [Fact]
    public async Task Submit_ValidAnswers_Returns200WithMatchedCharacter()
    {
        var request = new SubmitRequest
        {
            SessionId = Guid.NewGuid(),
            Answers = new Dictionary<string, int>
            {
                { "q1", 0 }, { "q2", 0 }, { "q3", 0 }, { "q4", 0 }, { "q5", 0 },
                { "q6", 0 }, { "q7", 0 }, { "q8", 0 }, { "q9", 0 }, { "q10", 0 }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/quiz/submit", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<SubmitResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body.MatchedCharacter));
        Assert.NotEqual(Guid.Empty, body.ResultId);
    }

    [Fact]
    public async Task Submit_EmptyAnswers_Returns400()
    {
        var request = new SubmitRequest
        {
            SessionId = Guid.NewGuid(),
            Answers = new Dictionary<string, int>()
        };

        var response = await _client.PostAsJsonAsync("/api/quiz/submit", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetResult_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/results/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

- [ ] **Step 4: Run all tests**

```bash
dotnet test
```
Expected: All tests pass. `Passed! - Failed: 0`

- [ ] **Step 5: Commit**

```bash
git add Ben10Api.Tests/
git commit -m "feat: add API integration tests"
```

---

## Task 9: React Frontend Scaffold

**Files:**
- Create: `ben10-frontend/` (Vite project)

- [ ] **Step 1: Scaffold Vite React project**

```bash
npm create vite@latest ben10-frontend -- --template react
cd ben10-frontend
npm install
npm install react-router-dom html2canvas
```

- [ ] **Step 2: Replace `src/main.jsx`**

```jsx
// ben10-frontend/src/main.jsx
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import App from './App.jsx'
import './styles/index.css'

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </StrictMode>
)
```

- [ ] **Step 3: Create `src/App.jsx` with routing**

```jsx
// ben10-frontend/src/App.jsx
import { Routes, Route } from 'react-router-dom'
import Home from './pages/Home.jsx'
import Quiz from './pages/Quiz.jsx'
import Loading from './pages/Loading.jsx'
import Result from './pages/Result.jsx'

export default function App() {
  return (
    <Routes>
      <Route path="/"           element={<Home />} />
      <Route path="/quiz"       element={<Quiz />} />
      <Route path="/loading"    element={<Loading />} />
      <Route path="/results/:id" element={<Result />} />
    </Routes>
  )
}
```

- [ ] **Step 4: Create `src/styles/index.css` (base responsive styles)**

```css
/* ben10-frontend/src/styles/index.css */
*, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

body {
  font-family: 'Segoe UI', sans-serif;
  background: #0a0a1a;
  color: #f0f0f0;
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.page {
  width: 100%;
  max-width: 640px;
  padding: 2rem 1rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1.5rem;
}

h1 { font-size: 2rem; text-align: center; color: #00e5ff; }
h2 { font-size: 1.4rem; text-align: center; }

.btn {
  background: #00e5ff;
  color: #0a0a1a;
  border: none;
  border-radius: 8px;
  padding: 0.75rem 2rem;
  font-size: 1rem;
  font-weight: 700;
  cursor: pointer;
  transition: opacity 0.2s;
}
.btn:hover { opacity: 0.85; }
.btn.secondary { background: transparent; border: 2px solid #00e5ff; color: #00e5ff; }

.error-text { color: #ff5252; text-align: center; }
```

- [ ] **Step 5: Verify dev server starts**

```bash
npm run dev
```
Expected: `Local: http://localhost:5173/` — blank page with no errors in console.

- [ ] **Step 6: Commit**

```bash
git add ben10-frontend/
git commit -m "feat: scaffold React Vite frontend with routing"
```

---

## Task 10: API Client

**Files:**
- Create: `ben10-frontend/src/api/quizApi.js`

- [ ] **Step 1: Create `quizApi.js`**

```js
// ben10-frontend/src/api/quizApi.js
const BASE = 'http://localhost:5000'

export async function fetchQuestions() {
  const res = await fetch(`${BASE}/api/quiz/questions`)
  if (!res.ok) throw new Error('Failed to load questions')
  return res.json()
}

export async function submitAnswers(sessionId, answers) {
  const res = await fetch(`${BASE}/api/quiz/submit`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ sessionId, answers })
  })
  if (!res.ok) {
    const err = await res.json().catch(() => ({}))
    throw new Error(err.error ?? 'Submission failed')
  }
  return res.json()
}

export async function fetchResult(id) {
  const res = await fetch(`${BASE}/api/results/${id}`)
  if (!res.ok) throw new Error('Result not found')
  return res.json()
}
```

- [ ] **Step 2: Commit**

```bash
git add ben10-frontend/src/api/
git commit -m "feat: add API client module"
```

---

## Task 11: Home + ProgressBar + Quiz Pages

**Files:**
- Create: `ben10-frontend/src/pages/Home.jsx`
- Create: `ben10-frontend/src/components/ProgressBar.jsx`
- Create: `ben10-frontend/src/pages/Quiz.jsx`
- Create: `ben10-frontend/src/pages/Loading.jsx`

- [ ] **Step 1: Create `Home.jsx`**

```jsx
// ben10-frontend/src/pages/Home.jsx
import { useNavigate } from 'react-router-dom'

export default function Home() {
  const navigate = useNavigate()
  return (
    <div className="page">
      <h1>Which Ben 10 Alien Are You?</h1>
      <p style={{ textAlign: 'center', color: '#aaa' }}>
        Answer 10 questions and discover your inner alien from the original Ben 10 series.
      </p>
      <button className="btn" onClick={() => navigate('/quiz')}>Start Quiz</button>
    </div>
  )
}
```

- [ ] **Step 2: Create `ProgressBar.jsx`**

```jsx
// ben10-frontend/src/components/ProgressBar.jsx
export default function ProgressBar({ current, total }) {
  const pct = Math.round((current / total) * 100)
  return (
    <div style={{ width: '100%', background: '#1a1a2e', borderRadius: 8, height: 10 }}>
      <div style={{
        width: `${pct}%`, background: '#00e5ff',
        height: '100%', borderRadius: 8, transition: 'width 0.3s'
      }} />
    </div>
  )
}
```

- [ ] **Step 3: Create `Quiz.jsx`**

```jsx
// ben10-frontend/src/pages/Quiz.jsx
import { useEffect, useState, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { fetchQuestions, submitAnswers } from '../api/quizApi.js'
import ProgressBar from '../components/ProgressBar.jsx'

export default function Quiz() {
  const [questions, setQuestions] = useState([])
  const [current, setCurrent]     = useState(0)
  const [answers, setAnswers]     = useState({})
  const [error, setError]         = useState(null)
  const sessionId                 = useRef(crypto.randomUUID())
  const navigate                  = useNavigate()

  useEffect(() => {
    fetchQuestions()
      .then(setQuestions)
      .catch(() => setError('Could not load questions. Please try again.'))
  }, [])

  function selectAnswer(questionId, index) {
    const next = { ...answers, [questionId]: index }
    setAnswers(next)

    if (current + 1 < questions.length) {
      setCurrent(current + 1)
    } else {
      navigate('/loading', { state: { sessionId: sessionId.current, answers: next } })
    }
  }

  if (error) return <div className="page"><p className="error-text">{error}</p></div>
  if (!questions.length) return <div className="page"><p>Loading questions…</p></div>

  const q = questions[current]
  return (
    <div className="page">
      <ProgressBar current={current + 1} total={questions.length} />
      <p style={{ color: '#aaa' }}>Question {current + 1} of {questions.length}</p>
      <h2>{q.text}</h2>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', width: '100%' }}>
        {q.answers.map((a, i) => (
          <button key={i} className="btn secondary" onClick={() => selectAnswer(q.id, i)}>
            {a.text}
          </button>
        ))}
      </div>
    </div>
  )
}
```

- [ ] **Step 4: Create `Loading.jsx`**

```jsx
// ben10-frontend/src/pages/Loading.jsx
import { useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { submitAnswers } from '../api/quizApi.js'

export default function Loading() {
  const { state }    = useLocation()
  const navigate     = useNavigate()
  const [error, setError] = useState(null)

  useEffect(() => {
    if (!state?.answers) { navigate('/'); return }

    submitAnswers(state.sessionId, state.answers)
      .then(result => navigate(`/results/${result.resultId}`, { state: { result } }))
      .catch(err   => setError(err.message))
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  if (error) return (
    <div className="page">
      <p className="error-text">{error}</p>
      <button className="btn" onClick={() => navigate('/quiz')}>Try Again</button>
    </div>
  )

  return (
    <div className="page">
      <p>Finding your alien match…</p>
    </div>
  )
}
```

- [ ] **Step 5: Commit**

```bash
git add ben10-frontend/src/
git commit -m "feat: add Home, Quiz, Loading pages and ProgressBar"
```

---

## Task 12: Result Page + Shareable Card

**Files:**
- Create: `ben10-frontend/src/components/ResultCard.jsx`
- Create: `ben10-frontend/src/pages/Result.jsx`

- [ ] **Step 1: Create `ResultCard.jsx`** (the DOM element captured by html2canvas)

```jsx
// ben10-frontend/src/components/ResultCard.jsx
export default function ResultCard({ character, description, imagePath, cardRef }) {
  return (
    <div ref={cardRef} style={{
      background: '#0a0a1a', border: '3px solid #00e5ff', borderRadius: 16,
      padding: '2rem', width: 360, display: 'flex', flexDirection: 'column',
      alignItems: 'center', gap: '1rem', color: '#f0f0f0'
    }}>
      <p style={{ color: '#00e5ff', fontSize: '0.85rem', fontWeight: 700 }}>
        BEN 10 PERSONALITY QUIZ
      </p>
      <img
        src={imagePath}
        alt={character}
        style={{ width: 180, height: 180, objectFit: 'contain' }}
        onError={e => { e.target.style.display = 'none' }}
      />
      <h2 style={{ color: '#00e5ff' }}>I got {character}!</h2>
      <p style={{ textAlign: 'center', fontSize: '0.9rem', color: '#ccc' }}>{description}</p>
    </div>
  )
}
```

- [ ] **Step 2: Create `Result.jsx`**

```jsx
// ben10-frontend/src/pages/Result.jsx
import { useEffect, useRef, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import html2canvas from 'html2canvas'
import { fetchResult } from '../api/quizApi.js'
import ResultCard from '../components/ResultCard.jsx'

export default function Result() {
  const { id }       = useParams()
  const { state }    = useLocation()
  const navigate     = useNavigate()
  const cardRef      = useRef(null)
  const [result, setResult] = useState(state?.result ?? null)
  const [error, setError]   = useState(null)

  useEffect(() => {
    if (!result) {
      fetchResult(id)
        .then(data => setResult({
          resultId: data.id,
          matchedCharacter: data.matchedCharacter,
          description: '',
          imagePath: `/images/aliens/${data.matchedCharacter.toLowerCase().replace(/\s+/g, '')}.png`,
          matchedTraits: []
        }))
        .catch(() => setError('Could not load result.'))
    }
  }, [id]) // eslint-disable-line react-hooks/exhaustive-deps

  async function downloadCard() {
    if (!cardRef.current) return
    const canvas = await html2canvas(cardRef.current, { backgroundColor: null })
    const link = document.createElement('a')
    link.download = `ben10-${result.matchedCharacter.toLowerCase()}.png`
    link.href = canvas.toDataURL('image/png')
    link.click()
  }

  if (error)  return <div className="page"><p className="error-text">{error}</p></div>
  if (!result) return <div className="page"><p>Loading…</p></div>

  return (
    <div className="page">
      <h1>You are {result.matchedCharacter}!</h1>
      {result.matchedTraits?.length > 0 && (
        <p style={{ color: '#aaa' }}>
          Your top traits: <strong>{result.matchedTraits.join(', ')}</strong>
        </p>
      )}
      <ResultCard
        character={result.matchedCharacter}
        description={result.description}
        imagePath={result.imagePath}
        cardRef={cardRef}
      />
      <button className="btn" onClick={downloadCard}>Download Card</button>
      <button className="btn secondary" onClick={() => navigate('/quiz')}>Retake Quiz</button>
      <p style={{ color: '#aaa', fontSize: '0.8rem' }}>
        Share link: {window.location.href}
      </p>
    </div>
  )
}
```

- [ ] **Step 3: Commit**

```bash
git add ben10-frontend/src/
git commit -m "feat: add Result page and shareable ResultCard component"
```

---

## Task 13: Manual End-to-End Verification

- [ ] **Step 1: Start the backend**

```bash
cd Ben10Api
dotnet run
```
Expected: API listening on `http://localhost:5000`

- [ ] **Step 2: Start the frontend**

```bash
cd ben10-frontend
npm run dev
```
Expected: `Local: http://localhost:5173/`

- [ ] **Step 3: Add alien images**

Copy or download the 10 alien PNGs into `ben10-frontend/public/images/aliens/`:
- `heatblast.png`, `wildmutt.png`, `diamondhead.png`, `xlr8.png`, `greymatter.png`
- `fourarms.png`, `stinkfly.png`, `ripjaws.png`, `upgrade.png`, `ghostfreak.png`

Source: Ben 10 Wiki (https://ben10.fandom.com) — fan/portfolio use only.

- [ ] **Step 4: Walk through full quiz flow**

1. Open `http://localhost:5173/`
2. Click "Start Quiz" — verify questions load one at a time with progress bar
3. Answer all 10 questions
4. Verify Loading screen appears briefly
5. Verify Result page shows matched alien name, description, and image
6. Click "Download Card" — verify PNG is downloaded
7. Copy the result URL and open in a new tab — verify result loads from the API

- [ ] **Step 5: Verify result saved in SQL Server**

```sql
SELECT * FROM QuizResults ORDER BY CreatedAt DESC;
```
Expected: One row per quiz submission with correct `MatchedCharacter`, `AnswersJson`, `TraitScoresJson`.

- [ ] **Step 6: Final commit**

```bash
git add .
git commit -m "chore: complete manual e2e verification"
```
