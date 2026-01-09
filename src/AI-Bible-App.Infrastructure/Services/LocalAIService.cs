using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System.Text;
using System.Runtime.CompilerServices;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// Implementation of AI service using local Phi-4 model via Ollama with RAG support
/// </summary>
public class LocalAIService : IAIService
{
    private OllamaApiClient? _client;
    private readonly string _modelName;
    private readonly string _ollamaUrl;
    private readonly int _numCtx;
    private readonly int _numPredict;
    private readonly ILogger<LocalAIService> _logger;
    private readonly IBibleRAGService? _ragService;
    private readonly bool _useRAG;
    private readonly Dictionary<string, string> _systemPromptCache = new();
    private readonly Dictionary<string, string> _ragContextCache = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);
    private readonly object _clientLock = new();

    public LocalAIService(
        IConfiguration configuration, 
        ILogger<LocalAIService> logger,
        IBibleRAGService? ragService = null)
    {
        _logger = logger;
        _ragService = ragService;
        
        _ollamaUrl = configuration["Ollama:Url"] ?? "http://localhost:11434";
        _modelName = configuration["Ollama:ModelName"] ?? "phi3:mini";
        _numCtx = int.TryParse(configuration["Ollama:NumCtx"], out var ctx) ? ctx : 2048;
        _numPredict = int.TryParse(configuration["Ollama:NumPredict"], out var pred) ? pred : 512;
        _useRAG = configuration["RAG:Enabled"] == "true" || configuration["RAG:Enabled"] == null;
        
        // DON'T create HttpClient/OllamaApiClient in constructor - causes WinUI3 crashes
        // Use lazy initialization instead
        
        _logger.LogInformation(
            "LocalAIService configured for model: {ModelName} at {Url}, RAG: {RAGEnabled}, NumCtx: {NumCtx}, NumPredict: {NumPredict}", 
            _modelName, 
            _ollamaUrl, 
            _useRAG && _ragService != null,
            _numCtx,
            _numPredict);
    }

    private OllamaApiClient GetClient()
    {
        if (_client == null)
        {
            lock (_clientLock)
            {
                if (_client == null)
                {
                    _logger.LogInformation("Lazy-initializing OllamaApiClient at {Url}...", _ollamaUrl);
                    var httpClient = new HttpClient
                    {
                        BaseAddress = new Uri(_ollamaUrl),
                        Timeout = TimeSpan.FromMinutes(5)
                    };
                    
                    _client = new OllamaApiClient(httpClient, _ollamaUrl)
                    {
                        SelectedModel = _modelName
                    };
                }
            }
        }
        return _client;
    }

    public async Task<string> GetChatResponseAsync(
        BiblicalCharacter character, 
        List<Core.Models.ChatMessage> conversationHistory, 
        string userMessage, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = new List<Message>
            {
                new Message
                {
                    Role = ChatRole.System,
                    Content = character.SystemPrompt
                }
            };

            // RAG: Retrieve relevant Bible verses if enabled
            string? retrievedContext = null;
            if (_useRAG && _ragService != null && _ragService.IsInitialized)
            {
                retrievedContext = await GetRelevantScriptureContextAsync(userMessage, cancellationToken);
                
                if (!string.IsNullOrEmpty(retrievedContext))
                {
                    // Add retrieved verses as context
                    messages.Add(new Message
                    {
                        Role = ChatRole.System,
                        Content = $"Relevant Scripture passages for context:\n{retrievedContext}\n\nUse these passages to inform your response when appropriate."
                    });
                    
                    _logger.LogDebug("Added RAG context to chat request");
                }
            }

            // Add conversation history (limit to last 6 messages for speed)
            foreach (var msg in conversationHistory.TakeLast(6))
            {
                messages.Add(new Message
                {
                    Role = msg.Role == "user" ? ChatRole.User : ChatRole.Assistant,
                    Content = msg.Content
                });
            }

            // Add current user message
            messages.Add(new Message
            {
                Role = ChatRole.User,
                Content = userMessage
            });

            var request = new ChatRequest
            {
                Model = _modelName,
                Messages = messages,
                Options = new RequestOptions
                {
                    NumCtx = _numCtx,
                    NumPredict = _numPredict
                }
            };

            _logger.LogDebug("Sending chat request to Ollama with {MessageCount} messages", messages.Count);

            var responseText = string.Empty;
            
            await foreach (var response in GetClient().ChatAsync(request, cancellationToken))
            {
                if (response?.Message?.Content != null)
                {
                    responseText += response.Message.Content;
                }
            }
            
            if (string.IsNullOrEmpty(responseText))
            {
                throw new InvalidOperationException("Received null or empty response from Ollama");
            }

            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat response from local AI model");
            throw;
        }
    }

    public async IAsyncEnumerable<string> StreamChatResponseAsync(
        BiblicalCharacter character, 
        List<Core.Models.ChatMessage> conversationHistory, 
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = new List<Message>
        {
            new Message
            {
                Role = ChatRole.System,
                Content = GetCachedSystemPrompt(character)
            }
        };

        // RAG: Retrieve relevant Bible verses if enabled (with caching)
        if (_useRAG && _ragService != null && _ragService.IsInitialized)
        {
            var retrievedContext = await GetCachedRelevantScriptureContextAsync(userMessage, cancellationToken);
            
            if (!string.IsNullOrEmpty(retrievedContext))
            {
                messages.Add(new Message
                {
                    Role = ChatRole.System,
                    Content = $"Relevant Scripture passages for context:\n{retrievedContext}\n\nUse these passages to inform your response when appropriate."
                });
            }
        }

        // Add conversation history (limit to last 6 messages for speed)
        foreach (var msg in conversationHistory.TakeLast(6))
        {
            messages.Add(new Message
            {
                Role = msg.Role == "user" ? ChatRole.User : ChatRole.Assistant,
                Content = msg.Content
            });
        }

        // Add current user message
        messages.Add(new Message
        {
            Role = ChatRole.User,
            Content = userMessage
        });

        var request = new ChatRequest
        {
            Model = _modelName,
            Messages = messages,
            Options = new RequestOptions
            {
                NumCtx = _numCtx,
                NumPredict = _numPredict
            }
        };

        _logger.LogDebug("Streaming chat response with {MessageCount} messages", messages.Count);

        await foreach (var response in GetClient().ChatAsync(request, cancellationToken))
        {
            if (response?.Message?.Content != null)
            {
                yield return response.Message.Content;
            }
        }
    }

    public async Task<string> GeneratePrayerAsync(string topic, CancellationToken cancellationToken = default)
    {
        try
        {
            var systemPrompt = @"You are a compassionate prayer writer. Generate heartfelt, biblical prayers that are meaningful and spiritually uplifting. 
Keep prayers concise (2-3 paragraphs), use reverent language, and include relevant scripture themes when appropriate.";

            var userPrompt = string.IsNullOrEmpty(topic) 
                ? "Generate a daily prayer for guidance, strength, and gratitude." 
                : $"Generate a prayer about: {topic}";

            var messages = new List<Message>
            {
                new Message
                {
                    Role = ChatRole.System,
                    Content = systemPrompt
                }
            };

            // RAG: Retrieve relevant Bible verses for prayer context
            if (_useRAG && _ragService != null && _ragService.IsInitialized)
            {
                var retrievedContext = await GetRelevantScriptureContextAsync(
                    topic ?? "daily prayer guidance strength gratitude", 
                    cancellationToken);
                
                if (!string.IsNullOrEmpty(retrievedContext))
                {
                    messages.Add(new Message
                    {
                        Role = ChatRole.System,
                        Content = $"Relevant Scripture passages to inspire the prayer:\n{retrievedContext}"
                    });
                    
                    _logger.LogDebug("Added RAG context to prayer generation");
                }
            }

            messages.Add(new Message
            {
                Role = ChatRole.User,
                Content = userPrompt
            });

            var request = new ChatRequest
            {
                Model = _modelName,
                Messages = messages
            };

            _logger.LogDebug("Generating prayer with topic: {Topic}", topic ?? "general");

            var responseText = string.Empty;
            
            await foreach (var response in GetClient().ChatAsync(request, cancellationToken))
            {
                if (response?.Message?.Content != null)
                {
                    responseText += response.Message.Content;
                }
            }
            
            if (string.IsNullOrEmpty(responseText))
            {
                throw new InvalidOperationException("Received null or empty response from Ollama");
            }

            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating prayer from local AI model");
            throw;
        }
    }

    /// <summary>
    /// Retrieve relevant Scripture context using RAG
    /// </summary>
    private async Task<string?> GetRelevantScriptureContextAsync(
        string query, 
        CancellationToken cancellationToken)
    {
        try
        {
            if (_ragService == null || !_ragService.IsInitialized)
            {
                return null;
            }

            var relevantChunks = await _ragService.RetrieveRelevantVersesAsync(
                query, 
                limit: 3,
                minRelevanceScore: 0.6,
                cancellationToken: cancellationToken);

            if (!relevantChunks.Any())
            {
                _logger.LogDebug("No relevant Scripture found for query: {Query}", query);
                return null;
            }

            var context = new StringBuilder();
            foreach (var chunk in relevantChunks)
            {
                context.AppendLine($"{chunk.Reference}:");
                context.AppendLine(chunk.Text);
                context.AppendLine();
            }

            _logger.LogInformation(
                "Retrieved {Count} relevant Scripture passages for query", 
                relevantChunks.Count);

            return context.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Scripture context");
            return null;
        }
    }

    /// <summary>
    /// Get cached system prompt for character
    /// </summary>
    private string GetCachedSystemPrompt(BiblicalCharacter character)
    {
        var cacheKey = $"prompt_{character.Name}";
        
        if (!_systemPromptCache.TryGetValue(cacheKey, out var cachedPrompt))
        {
            cachedPrompt = character.SystemPrompt;
            _systemPromptCache[cacheKey] = cachedPrompt;
            _logger.LogDebug("Cached system prompt for {Character}", character.Name);
        }
        
        return cachedPrompt;
    }

    /// <summary>
    /// Get cached RAG context (expires after 30 minutes)
    /// </summary>
    private async Task<string?> GetCachedRelevantScriptureContextAsync(
        string query, 
        CancellationToken cancellationToken)
    {
        var cacheKey = $"rag_{query.GetHashCode()}";
        
        if (_ragContextCache.TryGetValue(cacheKey, out var cachedContext))
        {
            _logger.LogDebug("Using cached RAG context for query");
            return cachedContext;
        }

        var context = await GetRelevantScriptureContextAsync(query, cancellationToken);
        
        if (!string.IsNullOrEmpty(context))
        {
            _ragContextCache[cacheKey] = context;
            _logger.LogDebug("Cached RAG context for query");
            
            // Clean up old cache entries (simple LRU with 100 item limit)
            if (_ragContextCache.Count > 100)
            {
                var oldestKey = _ragContextCache.Keys.First();
                _ragContextCache.Remove(oldestKey);
            }
        }
        
        return context;
    }
}
