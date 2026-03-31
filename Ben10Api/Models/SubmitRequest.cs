// Ben10Api/Models/SubmitRequest.cs
namespace Ben10Api.Models;

public class SubmitRequest
{
    public Guid SessionId { get; set; }
    // Key = question id (string), Value = answer index (int, 0-based)
    public Dictionary<string, int> Answers { get; set; } = new();
}
