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
