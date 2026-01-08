namespace AI_Bible_App.Core.Models;

/// <summary>
/// Represents a single message in a chat conversation
/// </summary>
public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Role { get; set; } = string.Empty; // "user", "assistant", "system"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string CharacterId { get; set; } = string.Empty;
    
    /// <summary>
    /// User rating for AI responses: -1 (thumbs down), 0 (no rating), 1 (thumbs up)
    /// Used for future model fine-tuning based on user feedback
    /// </summary>
    public int Rating { get; set; } = 0;
    
    /// <summary>
    /// Optional feedback text from user explaining the rating
    /// </summary>
    public string? Feedback { get; set; }
}
