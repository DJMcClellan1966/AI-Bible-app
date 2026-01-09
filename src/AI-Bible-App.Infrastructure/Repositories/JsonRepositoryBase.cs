using AI_Bible_App.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AI_Bible_App.Infrastructure.Repositories;

/// <summary>
/// Base class for JSON file-based repositories with optional encryption support.
/// Eliminates duplicate code across JsonChatRepository, JsonPrayerRepository, etc.
/// </summary>
/// <typeparam name="T">The entity type to store</typeparam>
public abstract class JsonRepositoryBase<T> where T : class
{
    protected readonly string DataDirectory;
    protected readonly string FilePath;
    protected readonly JsonSerializerOptions JsonOptions;
    protected readonly IEncryptionService? EncryptionService;
    protected readonly IFileSecurityService? FileSecurityService;
    protected readonly ILogger Logger;

    protected JsonRepositoryBase(
        ILogger logger,
        string fileName,
        IEncryptionService? encryptionService = null,
        IFileSecurityService? fileSecurityService = null,
        string dataDirectory = "data")
    {
        Logger = logger;
        EncryptionService = encryptionService;
        FileSecurityService = fileSecurityService;
        DataDirectory = dataDirectory;
        FilePath = Path.Combine(dataDirectory, fileName);
        JsonOptions = new JsonSerializerOptions { WriteIndented = true };
        
        // Ensure secure directory
        FileSecurityService?.EnsureSecureDirectory(dataDirectory);
        
        if (!Directory.Exists(dataDirectory))
            Directory.CreateDirectory(dataDirectory);
    }

    /// <summary>
    /// Gets the unique identifier for an entity (used for upsert operations)
    /// </summary>
    protected abstract string GetEntityId(T entity);

    /// <summary>
    /// Gets a user-friendly name for the entity type (used in log messages)
    /// </summary>
    protected abstract string EntityTypeName { get; }

    /// <summary>
    /// Loads all entities from the JSON file
    /// </summary>
    protected async Task<List<T>> LoadAllAsync()
    {
        if (!File.Exists(FilePath))
            return new List<T>();

        try
        {
            var json = await File.ReadAllTextAsync(FilePath);
            
            // Decrypt if encryption service available
            if (EncryptionService != null && EncryptionService.IsEncrypted(json))
            {
                json = EncryptionService.Decrypt(json);
            }
            
            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load {EntityType}s", EntityTypeName);
            return new List<T>();
        }
    }

    /// <summary>
    /// Saves all entities to the JSON file with optional encryption
    /// </summary>
    protected async Task SaveAllAsync(List<T> entities)
    {
        var json = JsonSerializer.Serialize(entities, JsonOptions);
        
        // Encrypt if encryption service available
        if (EncryptionService != null)
        {
            json = EncryptionService.Encrypt(json);
        }
        
        await File.WriteAllTextAsync(FilePath, json);
        
        // Set restrictive permissions
        FileSecurityService?.SetRestrictivePermissions(FilePath);
    }

    /// <summary>
    /// Saves or updates a single entity (upsert pattern)
    /// </summary>
    protected async Task UpsertAsync(T entity, string entityId)
    {
        try
        {
            var entities = await LoadAllAsync();
            var existingIndex = entities.FindIndex(e => GetEntityId(e) == entityId);
            
            if (existingIndex >= 0)
                entities[existingIndex] = entity;
            else
                entities.Add(entity);

            await SaveAllAsync(entities);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save {EntityType} {EntityId}", EntityTypeName, entityId);
            throw;
        }
    }

    /// <summary>
    /// Deletes an entity by ID
    /// </summary>
    protected async Task DeleteByIdAsync(string entityId)
    {
        try
        {
            var entities = await LoadAllAsync();
            entities.RemoveAll(e => GetEntityId(e) == entityId);
            await SaveAllAsync(entities);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete {EntityType} {EntityId}", EntityTypeName, entityId);
            throw;
        }
    }

    /// <summary>
    /// Gets a single entity by ID
    /// </summary>
    protected async Task<T> GetByIdAsync(string entityId, string notFoundMessage)
    {
        var entities = await LoadAllAsync();
        return entities.FirstOrDefault(e => GetEntityId(e) == entityId)
            ?? throw new KeyNotFoundException(notFoundMessage);
    }
}
