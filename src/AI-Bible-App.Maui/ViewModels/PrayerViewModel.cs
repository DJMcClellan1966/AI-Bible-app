using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

#pragma warning disable MVVMTK0045 // AOT compatibility warning for WinRT scenarios

namespace AI_Bible_App.Maui.ViewModels;

public partial class PrayerViewModel : BaseViewModel
{
    private readonly IAIService _aiService;
    private readonly IPrayerRepository _prayerRepository;

    [ObservableProperty]
    private string prayerRequest = string.Empty;

    [ObservableProperty]
    private string generatedPrayer = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Prayer> savedPrayers = new();

    [ObservableProperty]
    private bool isGenerating;

    public PrayerViewModel(IAIService aiService, IPrayerRepository prayerRepository)
    {
        _aiService = aiService;
        _prayerRepository = prayerRepository;
        Title = "Prayer Generator";
    }
    
    public async Task InitializeAsync()
    {
        await LoadSavedPrayersAsync();
    }

    private async Task LoadSavedPrayersAsync()
    {
        try
        {
            var prayers = await _prayerRepository.GetAllPrayersAsync();
            SavedPrayers = new ObservableCollection<Prayer>(prayers.OrderByDescending(p => p.CreatedAt));
        }
        catch (Exception ex)
        {
            // Only show alert if we have a valid page context
            if (Shell.Current?.CurrentPage != null)
            {
                await Shell.Current.CurrentPage.DisplayAlertAsync("Error", $"Failed to load prayers: {ex.Message}", "OK");
            }
        }
    }

    [RelayCommand]
    private async Task GeneratePrayer()
    {
        if (string.IsNullOrWhiteSpace(PrayerRequest) || IsGenerating)
            return;

        try
        {
            IsGenerating = true;
            GeneratedPrayer = "Generating prayer...";

            var prayer = await _aiService.GeneratePrayerAsync(PrayerRequest);
            GeneratedPrayer = prayer;
        }
        catch (Exception ex)
        {
            GeneratedPrayer = string.Empty;
            if (Shell.Current?.CurrentPage != null)
                await Shell.Current.CurrentPage.DisplayAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    private async Task SavePrayer()
    {
        if (string.IsNullOrWhiteSpace(GeneratedPrayer))
            return;

        try
        {
            var prayer = new Prayer
            {
                Id = Guid.NewGuid().ToString(),
                Topic = PrayerRequest,
                Content = GeneratedPrayer,
                CreatedAt = DateTime.UtcNow
            };

            await _prayerRepository.SavePrayerAsync(prayer);
            SavedPrayers.Insert(0, prayer);

            if (Shell.Current?.CurrentPage != null)
            {
                await Shell.Current.CurrentPage.DisplayAlertAsync(
                    "Success",
                    "Prayer saved successfully!",
                    "OK");
            }

            // Clear for new prayer
            PrayerRequest = string.Empty;
            GeneratedPrayer = string.Empty;
        }
        catch (Exception ex)
        {
            if (Shell.Current?.CurrentPage != null)
                await Shell.Current.CurrentPage.DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task ViewPrayer(Prayer prayer)
    {
        if (prayer == null)
            return;

        if (Shell.Current?.CurrentPage != null)
        {
            await Shell.Current.CurrentPage.DisplayAlertAsync(
                $"Prayer from {prayer.CreatedAt:MMMM d, yyyy}",
                $"Topic: {prayer.Topic}\n\n{prayer.Content}",
                "OK");
        }
    }

    [RelayCommand]
    private void NewPrayer()
    {
        PrayerRequest = string.Empty;
        GeneratedPrayer = string.Empty;
    }
}
