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

    [ObservableProperty]
    private bool useStreaming = true;

    public ChatViewModel(IAIService aiService, IChatRepository chatRepository)
    {
        _aiService = aiService;
        _chatRepository = chatRepository;
    }

    public async Task InitializeAsync(BiblicalCharacter character)
    {
        Character = character;
        Title = $"Chat with {character.Name}";
        
        // Create or load session
        _currentSession = new ChatSession
        {
            Id = Guid.NewGuid().ToString(),
            CharacterId = character.Id,
            StartedAt = DateTime.UtcNow,
            Messages = new List<ChatMessage>()
        };

        // Add welcome message
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
        if (string.IsNullOrWhiteSpace(UserMessage) || Character == null || IsAiTyping)
            return;

        try
        {
            IsAiTyping = true;
            var userMsg = UserMessage;
            UserMessage = string.Empty;

            // Add user message
            var chatMessage = new ChatMessage
            {
                Role = "user",
                Content = userMsg,
                Timestamp = DateTime.UtcNow
            };
            Messages.Add(chatMessage);
            _currentSession?.Messages.Add(chatMessage);

            // Get AI response with streaming
            if (UseStreaming)
            {
                // Create placeholder message for streaming
                var aiMessage = new ChatMessage
                {
                    Role = "assistant",
                    Content = "",
                    Timestamp = DateTime.UtcNow
                };
                Messages.Add(aiMessage);

                var conversationHistory = Messages.Take(Messages.Count - 1).ToList();
                
                await foreach (var token in _aiService.StreamChatResponseAsync(Character, conversationHistory, userMsg))
                {
                    aiMessage.Content += token;
                    // Trigger UI update
                    OnPropertyChanged(nameof(Messages));
                }

                _currentSession?.Messages.Add(aiMessage);
            }
            else
            {
                // Traditional non-streaming response
                var conversationHistory = Messages.ToList();
                var response = await _aiService.GetChatResponseAsync(Character, conversationHistory, userMsg);

                var aiMessage = new ChatMessage
                {
                    Role = "assistant",
                    Content = response,
                    Timestamp = DateTime.UtcNow
                };
                Messages.Add(aiMessage);
                _currentSession?.Messages.Add(aiMessage);
            }

            // Save session
            if (_currentSession != null)
            {
                await _chatRepository.SaveSessionAsync(_currentSession);
            }
        }
        catch (Exception ex)
        {
            var errorMessage = new ChatMessage
            {
                Role = "system",
                Content = $"Error: {ex.Message}",
                Timestamp = DateTime.UtcNow
            };
            Messages.Add(errorMessage);
        }
        finally
        {
            IsAiTyping = false;
        }
    }

    [RelayCommand]
    private async Task ClearChat()
    {
        var confirmed = false;
        if (Shell.Current?.CurrentPage != null)
        {
            confirmed = await Shell.Current.CurrentPage.DisplayAlertAsync(
                "Clear Chat",
                "Are you sure you want to clear this conversation?",
                "Yes", "No");
        }

        if (confirmed && Character != null)
        {
            Messages.Clear();
            await InitializeAsync(Character);
        }
    }
}
