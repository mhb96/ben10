// Ben10Api.Tests/QuizControllerIntegrationTests.cs
using System.Net;
using System.Net.Http.Json;
using Ben10Api.Data;
using Ben10Api.Models;
using Microsoft.AspNetCore.Hosting;
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
            builder.UseContentRoot(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Ben10Api"));
            builder.ConfigureServices(services =>
            {
                // Remove SQL Server DbContextOptions registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                // EF Core 8+ also registers IDbContextOptionsConfiguration<TContext>
                // per provider — remove those too so we don't end up with both
                // SqlServer and InMemory providers in the same options.
                var efConfigDescriptors = services
                    .Where(d => d.ServiceType.IsGenericType &&
                                d.ServiceType.GenericTypeArguments.Length == 1 &&
                                d.ServiceType.GenericTypeArguments[0] == typeof(AppDbContext) &&
                                d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration"))
                    .ToList();
                foreach (var d in efConfigDescriptors) services.Remove(d);

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
