using AI_Bible_App.Maui.ViewModels;

namespace AI_Bible_App.Maui.Views;

public partial class CharacterSelectionPage : ContentPage
{
    private readonly CharacterSelectionViewModel _viewModel;
    
    public CharacterSelectionPage(CharacterSelectionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
