using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using System.Text.Json;

namespace AI_Bible_App.Infrastructure.Repositories;

/// <summary>
/// JSON file-based implementation of chat repository
/// </summary>
public class JsonChatRepository : IChatRepository
{
    private readonly string _dataDirectory;
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonChatRepository(string dataDirectory = "data")
    {
        _dataDirectory = dataDirectory;
        _filePath = Path.Combine(_dataDirectory, "chat_sessions.json");
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        
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

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<ChatSession>>(json, _jsonOptions) ?? new List<ChatSession>();
    }

    public async Task SaveSessionAsync(ChatSession session)
    {
        var sessions = await GetAllSessionsAsync();
        var existingIndex = sessions.FindIndex(s => s.Id == session.Id);
        
        if (existingIndex >= 0)
            sessions[existingIndex] = session;
        else
            sessions.Add(session);

        var json = JsonSerializer.Serialize(sessions, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task DeleteSessionAsync(string sessionId)
    {
        var sessions = await GetAllSessionsAsync();
        sessions.RemoveAll(s => s.Id == sessionId);
        
        var json = JsonSerializer.Serialize(sessions, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
