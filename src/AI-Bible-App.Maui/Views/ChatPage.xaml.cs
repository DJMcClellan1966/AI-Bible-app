using AI_Bible_App.Core.Models;
using AI_Bible_App.Maui.ViewModels;

namespace AI_Bible_App.Maui.Views;

public partial class ChatPage : ContentPage, IQueryAttributable
{
    private readonly ChatViewModel _viewModel;

    public ChatPage(ChatViewModel viewModel)
    {
        System.Diagnostics.Debug.WriteLine("[DEBUG] ChatPage constructor START");
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        System.Diagnostics.Debug.WriteLine("[DEBUG] ChatPage constructor END");
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        System.Diagnostics.Debug.WriteLine("[DEBUG] ApplyQueryAttributes called");
        if (query.ContainsKey("character") && query["character"] is BiblicalCharacter character)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Initializing with character: {character.Name}");
            
            // Check if resuming an existing session
            ChatSession? existingSession = null;
            if (query.ContainsKey("session") && query["session"] is ChatSession session)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Resuming session: {session.Id}");
                existingSession = session;
            }
            
            await _viewModel.InitializeAsync(character, existingSession);
        }
    }
}
