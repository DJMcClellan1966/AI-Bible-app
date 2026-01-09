using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using AI_Bible_App.Maui.Services;
using CommunityToolkit.Maui.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using System.Collections.ObjectModel;
using System.Globalization;

#pragma warning disable MVVMTK0045 // AOT compatibility warning for WinRT scenarios

namespace AI_Bible_App.Maui.ViewModels;

public partial class ChatViewModel : BaseViewModel
{
    private readonly IAIService _aiService;
    private readonly IChatRepository _chatRepository;
    private readonly IBibleLookupService _bibleLookupService;
    private readonly IReflectionRepository _reflectionRepository;
    private readonly bool _enableContextualReferences;
    private ChatSession? _currentSession;
    private CancellationTokenSource? _speechCancellationTokenSource;

    [ObservableProperty]
    private BiblicalCharacter? character;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> messages = new();

    [ObservableProperty]
    private string userMessage = string.Empty;

    [ObservableProperty]
    private bool isAiTyping;

    [ObservableProperty]
    private bool isListening;

    public ChatViewModel(IAIService aiService, IChatRepository chatRepository, IBibleLookupService bibleLookupService, IReflectionRepository reflectionRepository, IConfiguration configuration)
    {
        _aiService = aiService;
        _chatRepository = chatRepository;
        _bibleLookupService = bibleLookupService;
        _reflectionRepository = reflectionRepository;
        _enableContextualReferences = configuration["Features:ContextualReferences"]?.ToLower() == "true";
    }

    public async Task InitializeAsync(BiblicalCharacter character, ChatSession? existingSession = null)
    {
        Character = character;
        Title = $"Chat with {character.Name}";
        
        // If no session passed, try to find existing session for this character
        if (existingSession == null)
        {
            existingSession = await _chatRepository.GetLatestSessionForCharacterAsync(character.Id);
        }
        
        if (existingSession != null)
        {
            // Resume existing session and update timestamp
            _currentSession = existingSession;
            _currentSession.StartedAt = DateTime.UtcNow; // Update to show it's active
            Messages = new ObservableCollection<ChatMessage>(existingSession.Messages);
        }
        else
        {
            // Start new session only if none exists
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
    }

    [RelayCommand]
    private async Task ToggleSpeechToText()
    {
        if (IsListening)
        {
            // Stop listening
            _speechCancellationTokenSource?.Cancel();
            IsListening = false;
            return;
        }

        try
        {
            // Check and request microphone permission
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    if (Shell.Current?.CurrentPage != null)
                    {
                        await Shell.Current.CurrentPage.DisplayAlert(
                            "Permission Required",
                            "Microphone permission is needed for speech-to-text.",
                            "OK");
                    }
                    return;
                }
            }

            IsListening = true;
            _speechCancellationTokenSource = new CancellationTokenSource();

            var recognitionResult = await SpeechToText.Default.ListenAsync(
                CultureInfo.CurrentCulture,
                new Progress<string>(partialText =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        UserMessage = partialText;
                    });
                }),
                _speechCancellationTokenSource.Token);

            if (recognitionResult.IsSuccessful)
            {
                UserMessage = recognitionResult.Text;
            }
            else if (!string.IsNullOrEmpty(recognitionResult.Text))
            {
                UserMessage = recognitionResult.Text;
            }
        }
        catch (OperationCanceledException)
        {
            // User cancelled - this is fine
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Speech recognition error: {ex.Message}");
            if (Shell.Current?.CurrentPage != null)
            {
                await Shell.Current.CurrentPage.DisplayAlert(
                    "Speech Error",
                    $"Could not recognize speech: {ex.Message}",
                    "OK");
            }
        }
        finally
        {
            IsListening = false;
            _speechCancellationTokenSource?.Dispose();
            _speechCancellationTokenSource = null;
        }
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

            // Create AI message placeholder for streaming
            var aiMessage = new ChatMessage
            {
                Role = "assistant",
                Content = "",
                Timestamp = DateTime.UtcNow
            };
            Messages.Add(aiMessage);

            System.Diagnostics.Debug.WriteLine("[DEBUG] Starting streaming response...");
            var conversationHistory = Messages.Where(m => m != aiMessage).ToList();
            
            // Stream the response on background thread, update UI on main thread
            await Task.Run(async () =>
            {
                await foreach (var token in _aiService.StreamChatResponseAsync(Character, conversationHistory, userMsg))
                {
                    // Update UI on main thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        aiMessage.Content += token;
                    });
                }
            });
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Streaming complete. Response length: {aiMessage.Content?.Length ?? 0}");
            
            _currentSession?.Messages.Add(aiMessage);

            // Fetch contextual Bible references in background (only if enabled)
            if (_enableContextualReferences)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var references = await _bibleLookupService.GetCharacterReferencesAsync(Character, userMsg);
                        if (references.Any())
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                aiMessage.ContextualReferences = references
                                    .Select(r => new ContextualReference
                                    {
                                        Reference = r.Reference,
                                        Summary = r.Summary,
                                        Connection = r.Connection
                                    })
                                    .ToList();
                            });
                            
                            // Save updated session with references
                            if (_currentSession != null)
                            {
                                await _chatRepository.SaveSessionAsync(_currentSession);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Error fetching contextual references: {ex.Message}");
                    }
                });
            }

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

    [RelayCommand]
    private async Task SaveToReflections(ChatMessage? message)
    {
        if (message == null || string.IsNullOrEmpty(message.Content)) return;

        try
        {
            if (Shell.Current?.CurrentPage != null)
            {
                var title = await Shell.Current.CurrentPage.DisplayPromptAsync(
                    "Save to Reflections",
                    "Give this reflection a title:",
                    initialValue: $"Chat with {Character?.Name ?? "Character"}",
                    maxLength: 100);

                if (title == null) return; // Cancelled

                // Get Bible references from contextual references if any
                var bibleRefs = message.ContextualReferences?
                    .Select(r => r.Reference)
                    .ToList() ?? new List<string>();

                var reflection = new Reflection
                {
                    Title = title,
                    SavedContent = message.Content,
                    Type = ReflectionType.Chat,
                    CharacterName = Character?.Name,
                    BibleReferences = bibleRefs,
                    CreatedAt = DateTime.UtcNow
                };

                await _reflectionRepository.SaveReflectionAsync(reflection);

                await Shell.Current.CurrentPage.DisplayAlert(
                    "Saved! ✓",
                    "This response has been saved to your reflections. You can add your personal thoughts there.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Error saving reflection: {ex.Message}");
        }
    }
}
