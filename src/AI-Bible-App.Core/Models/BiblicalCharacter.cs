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
    
    /// <summary>
    /// Filename of the character's icon (e.g., "david.png")
    /// </summary>
    public string IconFileName { get; set; } = "default_avatar.png";
    
    /// <summary>
    /// Voice configuration for text-to-speech reading of character responses
    /// </summary>
    public VoiceConfig Voice { get; set; } = new();
}

/// <summary>
/// Configuration for character voice in text-to-speech
/// </summary>
public class VoiceConfig
{
    /// <summary>
    /// Pitch of the voice (0.0 to 2.0, where 1.0 is normal)
    /// Lower values = deeper voice, Higher values = higher voice
    /// </summary>
    public float Pitch { get; set; } = 1.0f;
    
    /// <summary>
    /// Speech rate (0.0 to 2.0, where 1.0 is normal speed)
    /// </summary>
    public float Rate { get; set; } = 1.0f;
    
    /// <summary>
    /// Volume level (0.0 to 1.0)
    /// </summary>
    public float Volume { get; set; } = 1.0f;
    
    /// <summary>
    /// Description of the voice character (for UI display)
    /// e.g., "Kingly and authoritative", "Gentle shepherd", "Bold apostle"
    /// </summary>
    public string Description { get; set; } = "Default voice";
    
    /// <summary>
    /// Preferred locale for the voice (e.g., "en-US", "en-GB")
    /// </summary>
    public string Locale { get; set; } = "en-US";
}
