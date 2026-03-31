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
