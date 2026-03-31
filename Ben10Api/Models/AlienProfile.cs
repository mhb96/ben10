// Ben10Api/Models/AlienProfile.cs
namespace Ben10Api.Models;

public class AlienProfile
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public Dictionary<string, int> Traits { get; set; } = new();
}
