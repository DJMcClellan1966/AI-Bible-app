namespace AI_Bible_App.Core.Models;

/// <summary>
/// Represents a biblical character that can interact with users
/// </summary>
public class BiblicalCharacter
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Era { get; set; } = string.Empty;
    public List<string> BiblicalReferences { get; set; } = new();
    public string SystemPrompt { get; set; } = string.Empty;
    public Dictionary<string, string> Attributes { get; set; } = new();
}
