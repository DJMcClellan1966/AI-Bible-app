using AI_Bible_App.Maui.ViewModels;

namespace AI_Bible_App.Maui.Views;

public partial class PrayerPage : ContentPage
{
    public PrayerPage(PrayerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
