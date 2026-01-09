namespace AI_Bible_App.Core.Models;

/// <summary>
/// Represents an application user with their profile and settings
/// </summary>
public class AppUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? AvatarEmoji { get; set; } = "👤";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
    public UserSettings Settings { get; set; } = new();
    
    /// <summary>
    /// Optional PIN hash for privacy protection (null = no PIN required)
    /// </summary>
    public string? PinHash { get; set; }
    
    /// <summary>
    /// Whether this user has PIN protection enabled
    /// </summary>
    public bool HasPin => !string.IsNullOrEmpty(PinHash);
}

/// <summary>
/// User-specific settings and preferences
/// </summary>
public class UserSettings
{
    /// <summary>
    /// Whether content moderation is enabled (default: true)
    /// </summary>
    public bool EnableContentModeration { get; set; } = true;
    
    /// <summary>
    /// Preferred AI backend (local/cloud/auto)
    /// </summary>
    public string PreferredAIBackend { get; set; } = "auto";
    
    /// <summary>
    /// User's preferred Bible version (kjv, web, asv)
    /// </summary>
    public string PreferredBibleVersion { get; set; } = "kjv";
}
