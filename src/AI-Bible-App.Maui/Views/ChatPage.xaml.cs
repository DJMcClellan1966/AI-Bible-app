using AI_Bible_App.Core.Models;
using AI_Bible_App.Maui.ViewModels;

namespace AI_Bible_App.Maui.Views;

public partial class ChatPage : ContentPage, IQueryAttributable
{
    private readonly ChatViewModel _viewModel;

    public ChatPage(ChatViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("character") && query["character"] is BiblicalCharacter character)
        {
            await _viewModel.InitializeAsync(character);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Scroll to bottom when page appears
        ChatScrollView.ScrollToAsync(0, ChatScrollView.ContentSize.Height, false);
    }
}
