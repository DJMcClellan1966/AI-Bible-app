using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

#pragma warning disable MVVMTK0045 // AOT compatibility warning for WinRT scenarios

namespace AI_Bible_App.Maui.ViewModels;

public partial class ChatViewModel : BaseViewModel
{
    private readonly IAIService _aiService;
    private readonly IChatRepository _chatRepository;
    private ChatSession? _currentSession;

    [ObservableProperty]
    private BiblicalCharacter? character;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> messages = new();

    [ObservableProperty]
    private string userMessage = string.Empty;

    [ObservableProperty]
    private bool isAiTyping;

    public ChatViewModel(IAIService aiService, IChatRepository chatRepository)
    {
        _aiService = aiService;
        _chatRepository = chatRepository;
    }

    public async Task InitializeAsync(BiblicalCharacter character)
    {
        Character = character;
        Title = $"Chat with {character.Name}";
        
        _currentSession = new ChatSession
        {
            Id = Guid.NewGuid().ToString(),
            CharacterId = character.Id,
            StartedAt = DateTime.UtcNow,
            Messages = new List<ChatMessage>()
        };

        var welcomeMessage = new ChatMessage
        {
            Role = "assistant",
            Content = $"Peace be with you! I am {character.Name}, {character.Description}. How may I help you today?",
            Timestamp = DateTime.UtcNow
        };
        
        Messages.Add(welcomeMessage);
        _currentSession.Messages.Add(welcomeMessage);
    }

    [RelayCommand]
    private async Task SendMessage()
    {
        System.Diagnostics.Debug.WriteLine($"[DEBUG] SendMessage called. UserMessage: \"{UserMessage}\", Character: {Character?.Name}, IsAiTyping: {IsAiTyping}");
        
        if (string.IsNullOrWhiteSpace(UserMessage) || Character == null || IsAiTyping)
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] SendMessage early exit");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] Setting IsAiTyping = true");
            IsAiTyping = true;
            var userMsg = UserMessage;
            UserMessage = string.Empty;

            var chatMessage = new ChatMessage
            {
                Role = "user",
                Content = userMsg,
                Timestamp = DateTime.UtcNow
            };
            Messages.Add(chatMessage);
            _currentSession?.Messages.Add(chatMessage);
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Added user message: {userMsg}");

            System.Diagnostics.Debug.WriteLine("[DEBUG] Calling AI service...");
            var conversationHistory = Messages.ToList();
            var response = await _aiService.GetChatResponseAsync(Character, conversationHistory, userMsg);
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Got AI response length: {response?.Length ?? 0}");

            var aiMessage = new ChatMessage
            {
                Role = "assistant",
                Content = response ?? "No response received",
                Timestamp = DateTime.UtcNow
            };
            Messages.Add(aiMessage);
            _currentSession?.Messages.Add(aiMessage);

            if (_currentSession != null)
            {
                await _chatRepository.SaveSessionAsync(_currentSession);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] SendMessage ERROR: {ex}");
            var errorMessage = new ChatMessage
            {
                Role = "assistant",
                Content = $"Error: {ex.Message}",
                Timestamp = DateTime.UtcNow
            };
            Messages.Add(errorMessage);
        }
        finally
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] Setting IsAiTyping = false");
            IsAiTyping = false;
        }
    }

    [RelayCommand]
    private async Task RateMessage(ChatMessage message)
    {
        if (message == null || message.Role != "assistant") return;
        
        // Toggle rating: if already thumbs up, remove rating; otherwise set thumbs up
        message.Rating = message.Rating == 1 ? 0 : 1;
        System.Diagnostics.Debug.WriteLine($"[DEBUG] Rated message {message.Id} as {message.Rating}");
        
        // Save the updated session with rating
        if (_currentSession != null)
        {
            await _chatRepository.SaveSessionAsync(_currentSession);
        }
    }

    [RelayCommand]
    private async Task RateMessageNegative(ChatMessage message)
    {
        if (message == null || message.Role != "assistant") return;
        
        // Toggle rating: if already thumbs down, remove rating; otherwise set thumbs down
        message.Rating = message.Rating == -1 ? 0 : -1;
        System.Diagnostics.Debug.WriteLine($"[DEBUG] Rated message {message.Id} as {message.Rating}");
        
        // Save the updated session with rating
        if (_currentSession != null)
        {
            await _chatRepository.SaveSessionAsync(_currentSession);
        }
    }

    [RelayCommand]
    private async Task ClearChat()
    {
        if (Character != null)
        {
            Messages.Clear();
            await InitializeAsync(Character);
        }
    }
}
