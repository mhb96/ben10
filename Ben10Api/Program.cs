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
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.Run();

// NOTE: AppDbContext (Task 2) and IMatchingService/MatchingService (Task 5) are not yet defined.
// This file will not compile until those tasks are complete — this is expected.
