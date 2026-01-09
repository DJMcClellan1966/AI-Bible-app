namespace AI_Bible_App.Core.Models;

/// <summary>
/// Represents a chat conversation session with a biblical character
/// </summary>
public class ChatSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CharacterId { get; set; } = string.Empty;
    public List<ChatMessage> Messages { get; set; } = new();
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    
    /// <summary>
    /// User IDs this chat has been shared with (for family/group study)
    /// </summary>
    public List<string> SharedWithUserIds { get; set; } = new();
    
    /// <summary>
    /// Whether this chat is shared with all users on the device
    /// </summary>
    public bool IsSharedWithAll { get; set; } = false;
}
