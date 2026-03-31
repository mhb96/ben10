// Ben10Api/Program.cs
using Ben10Api.Data;
using Ben10Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Support Railway's DATABASE_URL env var or fallback to appsettings
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;
if (!string.IsNullOrEmpty(databaseUrl))
{
    // Railway provides postgres://user:pass@host:port/db
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!
        ?? throw new InvalidOperationException("No database connection string configured.");
}

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<IMatchingService, MatchingService>();

// Allow localhost dev + any Vercel deployment URL
var allowedOrigins = (Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "http://localhost:5173,https://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Auto-apply migrations on startup (safe for Railway deployments)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.Run();

// Required for WebApplicationFactory in tests
public partial class Program { }
