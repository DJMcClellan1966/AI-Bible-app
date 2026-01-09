using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

#pragma warning disable MVVMTK0045 // AOT compatibility warning for WinRT scenarios

namespace AI_Bible_App.Maui.ViewModels;

public partial class ChatHistoryViewModel : BaseViewModel
{
    private readonly IChatRepository _chatRepository;
    private readonly ICharacterRepository _characterRepository;

    [ObservableProperty]
    private ObservableCollection<ChatHistoryItem> chatSessions = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private ChatHistoryItem? selectedSession;

    public ChatHistoryViewModel(IChatRepository chatRepository, ICharacterRepository characterRepository)
    {
        _chatRepository = chatRepository;
        _characterRepository = characterRepository;
        Title = "Chat History";
    }

    partial void OnSelectedSessionChanged(ChatHistoryItem? value)
    {
        if (value != null)
        {
            // Fire and forget - navigate then clear selection
            _ = HandleSessionSelectedAsync(value);
        }
    }

    private async Task HandleSessionSelectedAsync(ChatHistoryItem item)
    {
        await ResumeChat(item);
        SelectedSession = null; // Clear selection for next time
    }

    public async Task InitializeAsync()
    {
        await LoadSessionsAsync();
    }

    [RelayCommand]
    private async Task LoadSessionsAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            var sessions = await _chatRepository.GetAllSessionsAsync();
            var characters = await _characterRepository.GetAllCharactersAsync();

            var historyItems = new List<ChatHistoryItem>();

            foreach (var session in sessions.OrderByDescending(s => s.StartedAt))
            {
                var character = characters.FirstOrDefault(c => c.Id == session.CharacterId);
                var lastMessage = session.Messages.LastOrDefault(m => m.Role == "assistant");
                
                historyItems.Add(new ChatHistoryItem
                {
                    Session = session,
                    CharacterName = character?.Name ?? "Unknown",
                    CharacterTitle = character?.Title ?? "",
                    LastMessage = lastMessage?.Content?.Length > 100 
                        ? lastMessage.Content.Substring(0, 100) + "..." 
                        : lastMessage?.Content ?? "No messages",
                    MessageCount = session.Messages.Count,
                    StartedAt = session.StartedAt
                });
            }

            ChatSessions = new ObservableCollection<ChatHistoryItem>(historyItems);
            IsEmpty = ChatSessions.Count == 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to load chat sessions: {ex}");
            if (Shell.Current?.CurrentPage != null)
            {
                await Shell.Current.CurrentPage.DisplayAlert("Error", $"Failed to load chat history: {ex.Message}", "OK");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ResumeChat(ChatHistoryItem item)
    {
        if (item?.Session == null) return;

        var characters = await _characterRepository.GetAllCharactersAsync();
        var character = characters.FirstOrDefault(c => c.Id == item.Session.CharacterId);

        if (character == null)
        {
            if (Shell.Current?.CurrentPage != null)
            {
                await Shell.Current.CurrentPage.DisplayAlert("Error", "Character not found", "OK");
            }
            return;
        }

        var navigationParams = new Dictionary<string, object>
        {
            { "character", character },
            { "session", item.Session }
        };

        await Shell.Current.GoToAsync("chat", navigationParams);
    }

    [RelayCommand]
    private async Task DeleteSession(ChatHistoryItem item)
    {
        if (item?.Session == null) return;

        bool confirm = await Shell.Current.CurrentPage.DisplayAlert(
            "Delete Chat",
            $"Delete conversation with {item.CharacterName}?",
            "Delete",
            "Cancel");

        if (confirm)
        {
            try
            {
                await _chatRepository.DeleteSessionAsync(item.Session.Id);
                ChatSessions.Remove(item);
                IsEmpty = ChatSessions.Count == 0;
            }
            catch (Exception ex)
            {
                if (Shell.Current?.CurrentPage != null)
                {
                    await Shell.Current.CurrentPage.DisplayAlert("Error", $"Failed to delete: {ex.Message}", "OK");
                }
            }
        }
    }

    [RelayCommand]
    private async Task ExportSession(ChatHistoryItem item)
    {
        if (item?.Session == null) return;

        try
        {
            // Build the text content
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"  CONVERSATION WITH {item.CharacterName.ToUpper()}");
            sb.AppendLine($"  {item.CharacterTitle}");
            sb.AppendLine($"═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"Started: {item.StartedAt:MMMM d, yyyy 'at' h:mm tt}");
            sb.AppendLine($"Messages: {item.MessageCount}");
            sb.AppendLine();
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            sb.AppendLine();

            foreach (var message in item.Session.Messages)
            {
                var speaker = message.Role == "user" ? "You" : item.CharacterName;
                var timestamp = message.Timestamp.ToLocalTime().ToString("h:mm tt");
                
                sb.AppendLine($"[{timestamp}] {speaker}:");
                sb.AppendLine(message.Content);
                sb.AppendLine();
            }

            sb.AppendLine("───────────────────────────────────────────────────────────────");
            sb.AppendLine($"Exported from Voices of Scripture on {DateTime.Now:MMMM d, yyyy}");

            var content = sb.ToString();
            var fileName = $"Chat_{item.CharacterName}_{item.StartedAt:yyyyMMdd_HHmmss}.txt";

            // Use Share API to let user save/share the file
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(filePath, content);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"Export conversation with {item.CharacterName}",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Export failed: {ex}");
            if (Shell.Current?.CurrentPage != null)
            {
                await Shell.Current.CurrentPage.DisplayAlert("Error", $"Failed to export: {ex.Message}", "OK");
            }
        }
    }

    [RelayCommand]
    private async Task GoToCharacters()
    {
        await Shell.Current.GoToAsync("//characters");
    }
}

/// <summary>
/// Display model for chat history items
/// </summary>
public class ChatHistoryItem
{
    public ChatSession Session { get; set; } = new();
    public string CharacterName { get; set; } = string.Empty;
    public string CharacterTitle { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public DateTime StartedAt { get; set; }
}
