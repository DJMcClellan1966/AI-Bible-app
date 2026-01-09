using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;

namespace AI_Bible_App.Maui.Services;

/// <summary>
/// Text-to-speech service for reading character responses with personalized voice settings
/// </summary>
public class CharacterVoiceService : ICharacterVoiceService
{
    private readonly ITextToSpeech _textToSpeech;
    private CancellationTokenSource? _currentSpeechCts;
    private bool _isSpeaking;

    public bool IsSpeaking => _isSpeaking;

    public CharacterVoiceService(ITextToSpeech textToSpeech)
    {
        _textToSpeech = textToSpeech;
    }

    public async Task SpeakAsync(string text, VoiceConfig voiceConfig, CancellationToken cancellationToken = default)
    {
        // Stop any ongoing speech
        await StopSpeakingAsync();

        // Clean the text - remove markdown formatting and emojis for cleaner speech
        var cleanedText = CleanTextForSpeech(text);
        
        if (string.IsNullOrWhiteSpace(cleanedText))
            return;

        _currentSpeechCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _isSpeaking = true;

        try
        {
            var options = new SpeechOptions
            {
                Pitch = voiceConfig.Pitch,
                Volume = voiceConfig.Volume
            };

            // Try to find a matching locale voice
            var locales = await _textToSpeech.GetLocalesAsync();
            var matchingLocale = locales.FirstOrDefault(l => 
                l.Language.StartsWith(voiceConfig.Locale.Split('-')[0], StringComparison.OrdinalIgnoreCase));
            
            if (matchingLocale != null)
            {
                options.Locale = matchingLocale;
            }

            await _textToSpeech.SpeakAsync(cleanedText, options, _currentSpeechCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Speech was cancelled - this is expected
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TTS Error: {ex.Message}");
        }
        finally
        {
            _isSpeaking = false;
            _currentSpeechCts?.Dispose();
            _currentSpeechCts = null;
        }
    }

    public Task StopSpeakingAsync()
    {
        if (_currentSpeechCts != null && !_currentSpeechCts.IsCancellationRequested)
        {
            _currentSpeechCts.Cancel();
        }
        _isSpeaking = false;
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<string>> GetAvailableLocalesAsync()
    {
        var locales = await _textToSpeech.GetLocalesAsync();
        return locales.Select(l => $"{l.Language}-{l.Country}").Distinct();
    }

    /// <summary>
    /// Cleans text for better speech synthesis - removes markdown, extra whitespace, etc.
    /// </summary>
    private static string CleanTextForSpeech(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var cleaned = text;

        // Remove markdown bold/italic
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\*\*(.+?)\*\*", "$1");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\*(.+?)\*", "$1");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"__(.+?)__", "$1");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"_(.+?)_", "$1");

        // Remove markdown headers
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^#{1,6}\s*", "", System.Text.RegularExpressions.RegexOptions.Multiline);

        // Remove markdown links [text](url) -> text
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\[(.+?)\]\(.+?\)", "$1");

        // Remove code blocks
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"```[\s\S]*?```", "");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"`(.+?)`", "$1");

        // Convert bullet points to pauses
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^[\-\*]\s+", ". ", System.Text.RegularExpressions.RegexOptions.Multiline);

        // Remove emojis (common emoji Unicode ranges)
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[\u{1F600}-\u{1F64F}]|[\u{1F300}-\u{1F5FF}]|[\u{1F680}-\u{1F6FF}]|[\u{1F1E0}-\u{1F1FF}]|[\u{2600}-\u{26FF}]|[\u{2700}-\u{27BF}]", "");

        // Normalize whitespace
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ");

        // Add pauses after scripture references for better flow
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"(\d+:\d+(?:-\d+)?)", "$1.");

        return cleaned.Trim();
    }
}
