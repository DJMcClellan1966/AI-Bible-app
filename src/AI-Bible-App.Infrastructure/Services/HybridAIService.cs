using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// Hybrid AI service that tries local Ollama first, falls back to Groq cloud
/// </summary>
public class HybridAIService : IAIService
{
    private readonly LocalAIService _localService;
    private readonly GroqAIService _cloudService;
    private readonly ILogger<HybridAIService> _logger;
    private readonly bool _preferLocal;
    private readonly bool _cloudAvailable;

    public HybridAIService(
        LocalAIService localService,
        GroqAIService cloudService,
        IConfiguration configuration,
        ILogger<HybridAIService> logger)
    {
        _localService = localService;
        _cloudService = cloudService;
        _logger = logger;
        _preferLocal = configuration["AI:PreferLocal"]?.ToLower() != "false";
        _cloudAvailable = _cloudService.IsAvailable;
        
        _logger.LogInformation(
            "HybridAIService initialized. PreferLocal: {PreferLocal}, CloudAvailable: {CloudAvailable}",
            _preferLocal, _cloudAvailable);
    }

    public async Task<string> GetChatResponseAsync(
        BiblicalCharacter character,
        List<ChatMessage> conversationHistory,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        if (_preferLocal)
        {
            try
            {
                _logger.LogDebug("Trying local AI service...");
                return await _localService.GetChatResponseAsync(character, conversationHistory, userMessage, cancellationToken);
            }
            catch (Exception ex) when (_cloudAvailable)
            {
                _logger.LogWarning(ex, "Local AI failed, falling back to cloud");
                return await _cloudService.GetChatResponseAsync(character, conversationHistory, userMessage, cancellationToken);
            }
        }
        else if (_cloudAvailable)
        {
            try
            {
                _logger.LogDebug("Trying cloud AI service...");
                return await _cloudService.GetChatResponseAsync(character, conversationHistory, userMessage, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cloud AI failed, falling back to local");
                return await _localService.GetChatResponseAsync(character, conversationHistory, userMessage, cancellationToken);
            }
        }
        else
        {
            return await _localService.GetChatResponseAsync(character, conversationHistory, userMessage, cancellationToken);
        }
    }

    public async IAsyncEnumerable<string> StreamChatResponseAsync(
        BiblicalCharacter character,
        List<ChatMessage> conversationHistory,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // For hybrid, prefer local streaming, fallback to cloud non-streaming
        if (_preferLocal)
        {
            var localFailed = false;
            var errorMessage = "";
            
            await foreach (var token in TryLocalStreamAsync(character, conversationHistory, userMessage, cancellationToken))
            {
                if (token.StartsWith("__ERROR__:"))
                {
                    localFailed = true;
                    errorMessage = token.Replace("__ERROR__:", "");
                    break;
                }
                yield return token;
            }

            if (localFailed && _cloudAvailable)
            {
                _logger.LogWarning("Local streaming failed: {Error}, falling back to cloud", errorMessage);
                var response = await _cloudService.GetChatResponseAsync(character, conversationHistory, userMessage, cancellationToken);
                yield return response;
            }
        }
        else
        {
            // Cloud first
            var response = await GetChatResponseAsync(character, conversationHistory, userMessage, cancellationToken);
            yield return response;
        }
    }

    private async IAsyncEnumerable<string> TryLocalStreamAsync(
        BiblicalCharacter character,
        List<ChatMessage> conversationHistory,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var enumerator = _localService.StreamChatResponseAsync(character, conversationHistory, userMessage, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        
        string? errorMessage = null;
        
        while (true)
        {
            string? current = null;
            bool hasNext = false;
            
            try
            {
                hasNext = await enumerator.MoveNextAsync();
                if (hasNext)
                    current = enumerator.Current;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                break;
            }
            
            if (!hasNext)
                break;
                
            if (current != null)
                yield return current;
        }
        
        if (errorMessage != null)
        {
            yield return $"__ERROR__:{errorMessage}";
        }
    }

    public async Task<string> GeneratePrayerAsync(string topic, CancellationToken cancellationToken = default)
    {
        if (_preferLocal)
        {
            try
            {
                return await _localService.GeneratePrayerAsync(topic, cancellationToken);
            }
            catch (Exception ex) when (_cloudAvailable)
            {
                _logger.LogWarning(ex, "Local prayer generation failed, falling back to cloud");
                return await _cloudService.GeneratePrayerAsync(topic, cancellationToken);
            }
        }
        else if (_cloudAvailable)
        {
            try
            {
                return await _cloudService.GeneratePrayerAsync(topic, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cloud prayer generation failed, falling back to local");
                return await _localService.GeneratePrayerAsync(topic, cancellationToken);
            }
        }
        
        return await _localService.GeneratePrayerAsync(topic, cancellationToken);
    }
}
