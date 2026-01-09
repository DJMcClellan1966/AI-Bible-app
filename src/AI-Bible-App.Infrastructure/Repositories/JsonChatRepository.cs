using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AI_Bible_App.Infrastructure.Repositories;

/// <summary>
/// JSON file-based implementation of chat repository with encryption
/// </summary>
public class JsonChatRepository : IChatRepository
{
    private readonly string _dataDirectory;
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IEncryptionService? _encryptionService;
    private readonly IFileSecurityService? _fileSecurityService;
    private readonly ILogger<JsonChatRepository> _logger;

    public JsonChatRepository(
        ILogger<JsonChatRepository> logger,
        IEncryptionService? encryptionService = null,
        IFileSecurityService? fileSecurityService = null,
        string dataDirectory = "data")
    {
        _logger = logger;
        _encryptionService = encryptionService;
        _fileSecurityService = fileSecurityService;
        _dataDirectory = dataDirectory;
        _filePath = Path.Combine(_dataDirectory, "chat_sessions.json");
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        
        // Ensure secure directory
        _fileSecurityService?.EnsureSecureDirectory(_dataDirectory);
        
        if (!Directory.Exists(_dataDirectory))
            Directory.CreateDirectory(_dataDirectory);
    }

    public async Task<ChatSession> GetSessionAsync(string sessionId)
    {
        var sessions = await GetAllSessionsAsync();
        return sessions.FirstOrDefault(s => s.Id == sessionId) 
            ?? throw new KeyNotFoundException($"Session {sessionId} not found");
    }

    public async Task<List<ChatSession>> GetAllSessionsAsync()
    {
        if (!File.Exists(_filePath))
            return new List<ChatSession>();

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            
            // Decrypt if encryption service available
            if (_encryptionService != null && _encryptionService.IsEncrypted(json))
            {
                json = _encryptionService.Decrypt(json);
            }
            
            return JsonSerializer.Deserialize<List<ChatSession>>(json, _jsonOptions) ?? new List<ChatSession>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load chat sessions");
            return new List<ChatSession>();
        }
    }

    public async Task SaveSessionAsync(ChatSession session)
    {
        try
        {
            var sessions = await GetAllSessionsAsync();
            var existingIndex = sessions.FindIndex(s => s.Id == session.Id);
            
            if (existingIndex >= 0)
                sessions[existingIndex] = session;
            else
                sessions.Add(session);

            var json = JsonSerializer.Serialize(sessions, _jsonOptions);
            
            // Encrypt if encryption service available
            if (_encryptionService != null)
            {
                json = _encryptionService.Encrypt(json);
            }
            
            await File.WriteAllTextAsync(_filePath, json);
            
            // Set restrictive permissions
            _fileSecurityService?.SetRestrictivePermissions(_filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save chat session {SessionId}", session.Id);
            throw;
        }
    }

    public async Task<ChatSession?> GetLatestSessionForCharacterAsync(string characterId)
    {
        var sessions = await GetAllSessionsAsync();
        return sessions
            .Where(s => s.CharacterId == characterId)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefault();
    }

    public async Task DeleteSessionAsync(string sessionId)
    {
        try
        {
            var sessions = await GetAllSessionsAsync();
            sessions.RemoveAll(s => s.Id == sessionId);
            
            var json = JsonSerializer.Serialize(sessions, _jsonOptions);
            
            // Encrypt if encryption service available
            if (_encryptionService != null)
            {
                json = _encryptionService.Encrypt(json);
            }
            
            await File.WriteAllTextAsync(_filePath, json);
            
            // Set restrictive permissions
            _fileSecurityService?.SetRestrictivePermissions(_filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete chat session {SessionId}", sessionId);
            throw;
        }
    }
}
