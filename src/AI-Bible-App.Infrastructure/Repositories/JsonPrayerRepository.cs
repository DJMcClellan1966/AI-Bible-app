using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using System.Text.Json;

namespace AI_Bible_App.Infrastructure.Repositories;

/// <summary>
/// JSON file-based implementation of prayer repository
/// </summary>
public class JsonPrayerRepository : IPrayerRepository
{
    private readonly string _dataDirectory;
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonPrayerRepository(string dataDirectory = "data")
    {
        _dataDirectory = dataDirectory;
        _filePath = Path.Combine(_dataDirectory, "prayers.json");
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        
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

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<Prayer>>(json, _jsonOptions) ?? new List<Prayer>();
    }

    public async Task<List<Prayer>> GetPrayersByTopicAsync(string topic)
    {
        var prayers = await GetAllPrayersAsync();
        return prayers.Where(p => p.Topic.Contains(topic, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task SavePrayerAsync(Prayer prayer)
    {
        var prayers = await GetAllPrayersAsync();
        var existingIndex = prayers.FindIndex(p => p.Id == prayer.Id);
        
        if (existingIndex >= 0)
            prayers[existingIndex] = prayer;
        else
            prayers.Add(prayer);

        var json = JsonSerializer.Serialize(prayers, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task DeletePrayerAsync(string prayerId)
    {
        var prayers = await GetAllPrayersAsync();
        prayers.RemoveAll(p => p.Id == prayerId);
        
        var json = JsonSerializer.Serialize(prayers, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
