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
    private readonly IDialogService _dialogService;
    private readonly IContentModerationService _moderationService;
    private readonly IUserService _userService;
    private readonly ICharacterVoiceService _voiceService;
    private readonly bool _enableContextualReferences;
    private ChatSession? _currentSession;
    private CancellationTokenSource? _speechCancellationTokenSource;
    private CancellationTokenSource? _aiResponseCancellationTokenSource;
    private CancellationTokenSource? _voiceCancellationTokenSource;
    private ChatMessage? _currentlySpeakingMessage;

    // Event to request scroll to bottom
    public event EventHandler? ScrollToBottomRequested;

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

    [ObservableProperty]
    private bool isSpeaking;

    [ObservableProperty]
    private string? speakingMessageId;

    public ChatViewModel(IAIService aiService, IChatRepository chatRepository, IBibleLookupService bibleLookupService, IReflectionRepository reflectionRepository, IDialogService dialogService, IContentModerationService moderationService, IUserService userService, ICharacterVoiceService voiceService, IConfiguration configuration)
    {
        _aiService = aiService;
        _chatRepository = chatRepository;
        _bibleLookupService = bibleLookupService;
        _reflectionRepository = reflectionRepository;
        _dialogService = dialogService;
        _moderationService = moderationService;
        _userService = userService;
        _voiceService = voiceService;
        _enableContextualReferences = configuration["Features:ContextualReferences"]?.ToLower() == "true";
    }

    public async Task InitializeAsync(BiblicalCharacter character, ChatSession? existingSession = null, bool forceNewChat = false)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] ChatViewModel.InitializeAsync START - character: {character.Name}, forceNewChat: {forceNewChat}");
            
            Character = character;
            Title = $"Chat with {character.Name}";
            
            // If forcing new chat, don't look for existing session
            if (!forceNewChat && existingSession == null)
            {
                existingSession = await _chatRepository.GetLatestSessionForCharacterAsync(character.Id);
            }
            
            if (!forceNewChat && existingSession != null)
            {
                // Resume existing session and update timestamp
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Resuming existing session with {existingSession.Messages.Count} messages");
                _currentSession = existingSession;
                _currentSession.StartedAt = DateTime.UtcNow; // Update to show it's active
                Messages = new ObservableCollection<ChatMessage>(existingSession.Messages);
            }
            else
            {
                // Start new session
                System.Diagnostics.Debug.WriteLine("[DEBUG] Creating new chat session");
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
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] ChatViewModel.InitializeAsync END - messages: {Messages.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] ChatViewModel.InitializeAsync FAILED: {ex}");
            throw;
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
                    await _dialogService.ShowAlertAsync(
                        "Permission Required",
                        "Microphone permission is needed for speech-to-text.");
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
            await _dialogService.ShowAlertAsync(
                "Speech Error",
                $"Could not recognize speech: {ex.Message}");
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
            // Content moderation check (if enabled for user)
            var shouldModerate = _userService.CurrentUser?.Settings.EnableContentModeration ?? true;
            if (shouldModerate)
            {
                var moderationResult = _moderationService.CheckContent(UserMessage);
                if (!moderationResult.IsAppropriate)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Message blocked by moderation: {moderationResult.Reason}");
                    await _dialogService.ShowAlertAsync(
                        "Message Not Sent",
                        moderationResult.Reason ?? "Please rephrase your message with appropriate language.");
                    return;
                }
            }

            System.Diagnostics.Debug.WriteLine("[DEBUG] Setting IsAiTyping = true");
            IsAiTyping = true;
            _aiResponseCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _aiResponseCancellationTokenSource.Token;
            
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
            
            // Request scroll to bottom after user message
            ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);

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
            try
            {
                await foreach (var token in _aiService.StreamChatResponseAsync(Character, conversationHistory, userMsg, cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    
                    // Update UI on main thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        aiMessage.Content += token;
                    });
                }
            }
            catch (Exception streamEx)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Streaming ERROR: {streamEx}");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    aiMessage.Content += $"\n\n[Connection error - please try again]";
                });
            }
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Streaming complete. Response length: {aiMessage.Content?.Length ?? 0}");
            
            // Request scroll to bottom after AI response
            ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
            
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
            _aiResponseCancellationTokenSource?.Dispose();
            _aiResponseCancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private void CancelResponse()
    {
        if (_aiResponseCancellationTokenSource != null && !_aiResponseCancellationTokenSource.IsCancellationRequested)
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] Cancelling AI response...");
            _aiResponseCancellationTokenSource.Cancel();
        }
    }

    [RelayCommand]
    private async Task ToggleVoice(ChatMessage? message)
    {
        System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleVoice called, message is null: {message == null}");
        
        if (message == null || string.IsNullOrEmpty(message.Content) || message.Role != "assistant")
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleVoice early return - message null: {message == null}, content empty: {string.IsNullOrEmpty(message?.Content)}, role: {message?.Role}");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleVoice proceeding with message: {message.Content.Substring(0, Math.Min(50, message.Content.Length))}...");

        // If we're already speaking this message, stop it
        if (IsSpeaking && SpeakingMessageId == message.Id)
        {
            await StopSpeakingAsync();
            return;
        }

        // If speaking a different message, stop that first
        if (IsSpeaking)
        {
            await StopSpeakingAsync();
        }

        // Start speaking this message
        await SpeakMessageAsync(message);
    }

    private async Task SpeakMessageAsync(ChatMessage message)
    {
        if (Character == null) return;

        try
        {
            IsSpeaking = true;
            SpeakingMessageId = message.Id;
            _currentlySpeakingMessage = message;
            _voiceCancellationTokenSource = new CancellationTokenSource();

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Speaking message with voice: {Character.Voice.Description}");
            
            await _voiceService.SpeakAsync(
                message.Content,
                Character.Voice,
                _voiceCancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when user cancels
            System.Diagnostics.Debug.WriteLine("[DEBUG] Voice playback cancelled by user");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Voice playback error: {ex.Message}");
            await _dialogService.ShowAlertAsync("Voice Error", $"Could not play voice: {ex.Message}");
        }
        finally
        {
            IsSpeaking = false;
            SpeakingMessageId = null;
            _currentlySpeakingMessage = null;
            _voiceCancellationTokenSource?.Dispose();
            _voiceCancellationTokenSource = null;
        }
    }

    private async Task StopSpeakingAsync()
    {
        if (_voiceCancellationTokenSource != null && !_voiceCancellationTokenSource.IsCancellationRequested)
        {
            _voiceCancellationTokenSource.Cancel();
        }
        await _voiceService.StopSpeakingAsync();
        IsSpeaking = false;
        SpeakingMessageId = null;
        _currentlySpeakingMessage = null;
    }

    [RelayCommand]
    private async Task StopVoice()
    {
        await StopSpeakingAsync();
    }

    [RelayCommand]
    private async Task RateMessage((ChatMessage message, int targetRating) args)
    {
        var (message, targetRating) = args;
        if (message == null || message.Role != "assistant") return;
        
        // Toggle rating: if already at target rating, remove it; otherwise set it
        var newRating = message.Rating == targetRating ? 0 : targetRating;
        message.Rating = newRating;
        System.Diagnostics.Debug.WriteLine($"[DEBUG] Rated message {message.Id} as {message.Rating}");
        
        // If rated (not removing rating), optionally ask for feedback
        if (newRating != 0)
        {
            var provideFeedback = await _dialogService.ShowConfirmAsync(
                "Feedback",
                "Would you like to explain why?",
                "Yes", "No");
            
            if (provideFeedback)
            {
                var prompt = newRating == 1 
                    ? "What made this response helpful?" 
                    : "How could this response be improved?";
                    
                var feedback = await _dialogService.ShowPromptAsync(
                    "Your Feedback",
                    prompt,
                    maxLength: 500);
                
                if (!string.IsNullOrWhiteSpace(feedback))
                {
                    message.Feedback = feedback;
                }
            }
        }
        else
        {
            // Clearing rating also clears feedback
            message.Feedback = null;
        }
        
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
            var title = await _dialogService.ShowPromptAsync(
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

            await _dialogService.ShowAlertAsync(
                "Saved! ✓",
                "This response has been saved to your reflections. You can add your personal thoughts there.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Error saving reflection: {ex.Message}");
        }
    }
}
