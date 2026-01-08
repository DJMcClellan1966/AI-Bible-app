using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AI_Bible_App.Infrastructure.Repositories;

/// <summary>
/// JSON file-based implementation of prayer repository with encryption
/// </summary>
public class JsonPrayerRepository : IPrayerRepository
{
    private readonly string _dataDirectory;
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IEncryptionService? _encryptionService;
    private readonly IFileSecurityService? _fileSecurityService;
    private readonly ILogger<JsonPrayerRepository> _logger;

    public JsonPrayerRepository(
        ILogger<JsonPrayerRepository> logger,
        IEncryptionService? encryptionService = null,
        IFileSecurityService? fileSecurityService = null,
        string dataDirectory = "data")
    {
        _logger = logger;
        _encryptionService = encryptionService;
        _fileSecurityService = fileSecurityService;
        _dataDirectory = dataDirectory;
        _filePath = Path.Combine(_dataDirectory, "prayers.json");
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        
        // Ensure secure directory
        _fileSecurityService?.EnsureSecureDirectory(_dataDirectory);
        
        if (!Directory.Exists(_dataDirectory))
            Directory.CreateDirectory(_dataDirectory);
    }

    public async Task<Prayer> GetPrayerAsync(string prayerId)
    {
        var prayers = await GetAllPrayersAsync();
        return prayers.FirstOrDefault(p => p.Id == prayerId) 
            ?? throw new KeyNotFoundException($"Prayer {prayerId} not found");
    }

    public async Task<List<Prayer>> GetAllPrayersAsync()
    {
        if (!File.Exists(_filePath))
            return new List<Prayer>();

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            
            // Decrypt if encryption service available
            if (_encryptionService != null && _encryptionService.IsEncrypted(json))
            {
                json = _encryptionService.Decrypt(json);
            }
            
            return JsonSerializer.Deserialize<List<Prayer>>(json, _jsonOptions) ?? new List<Prayer>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load prayers");
            return new List<Prayer>();
        }
    }

    public async Task<List<Prayer>> GetPrayersByTopicAsync(string topic)
    {
        var prayers = await GetAllPrayersAsync();
        return prayers.Where(p => p.Topic.Contains(topic, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task SavePrayerAsync(Prayer prayer)
    {
        try
        {
            var prayers = await GetAllPrayersAsync();
            var existingIndex = prayers.FindIndex(p => p.Id == prayer.Id);
            
            if (existingIndex >= 0)
                prayers[existingIndex] = prayer;
            else
                prayers.Add(prayer);

            var json = JsonSerializer.Serialize(prayers, _jsonOptions);
            
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
            _logger.LogError(ex, "Failed to save prayer {PrayerId}", prayer.Id);
            throw;
        }
    }

    public async Task DeletePrayerAsync(string prayerId)
    {
        try
        {
            var prayers = await GetAllPrayersAsync();
            prayers.RemoveAll(p => p.Id == prayerId);
            
            var json = JsonSerializer.Serialize(prayers, _jsonOptions);
            
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
            _logger.LogError(ex, "Failed to delete prayer {PrayerId}", prayerId);
            throw;
        }
    }
}
