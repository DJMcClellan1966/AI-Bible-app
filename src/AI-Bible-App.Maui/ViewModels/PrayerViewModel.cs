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
    private readonly IReflectionRepository _reflectionRepository;

    [ObservableProperty]
    private string prayerRequest = string.Empty;

    [ObservableProperty]
    private string generatedPrayer = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Prayer> savedPrayers = new();

    [ObservableProperty]
    private Prayer? selectedPrayer;

    [ObservableProperty]
    private bool isGenerating;

    public PrayerViewModel(IAIService aiService, IPrayerRepository prayerRepository, IReflectionRepository reflectionRepository)
    {
        _aiService = aiService;
        _prayerRepository = prayerRepository;
        _reflectionRepository = reflectionRepository;
        Title = "Prayer Generator";
    }

    partial void OnSelectedPrayerChanged(Prayer? value)
    {
        if (value != null)
        {
            _ = HandlePrayerSelectedAsync(value);
        }
    }

    private async Task HandlePrayerSelectedAsync(Prayer prayer)
    {
        await ViewPrayer(prayer);
        SelectedPrayer = null; // Clear selection for next time
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
                await Shell.Current.CurrentPage.DisplayAlert("Error", $"Failed to load prayers: {ex.Message}", "OK");
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
                await Shell.Current.CurrentPage.DisplayAlert("Error", ex.Message, "OK");
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
                await Shell.Current.CurrentPage.DisplayAlert(
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
                await Shell.Current.CurrentPage.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task ViewPrayer(Prayer prayer)
    {
        if (prayer == null)
            return;

        if (Shell.Current?.CurrentPage != null)
        {
            // Convert UTC to local time for display
            var localTime = prayer.CreatedAt.Kind == DateTimeKind.Utc 
                ? prayer.CreatedAt.ToLocalTime() 
                : DateTime.SpecifyKind(prayer.CreatedAt, DateTimeKind.Utc).ToLocalTime();
            
            await Shell.Current.CurrentPage.DisplayAlert(
                $"Prayer: {prayer.Topic}",
                $"{prayer.Content}\n\n— Saved {localTime:MMMM d, yyyy h:mm tt}",
                "Close");
        }
    }

    [RelayCommand]
    private void NewPrayer()
    {
        PrayerRequest = string.Empty;
        GeneratedPrayer = string.Empty;
    }

    [RelayCommand]
    private async Task SaveToReflections()
    {
        if (string.IsNullOrWhiteSpace(GeneratedPrayer)) return;

        try
        {
            if (Shell.Current?.CurrentPage != null)
            {
                var title = await Shell.Current.CurrentPage.DisplayPromptAsync(
                    "Save to Reflections",
                    "Give this reflection a title:",
                    initialValue: $"Prayer: {(string.IsNullOrEmpty(PrayerRequest) ? "Daily Prayer" : PrayerRequest)}",
                    maxLength: 100);

                if (title == null) return; // Cancelled

                var reflection = new Reflection
                {
                    Title = title,
                    SavedContent = GeneratedPrayer,
                    Type = ReflectionType.Prayer,
                    CreatedAt = DateTime.UtcNow
                };

                await _reflectionRepository.SaveReflectionAsync(reflection);

                await Shell.Current.CurrentPage.DisplayAlert(
                    "Saved! ✓",
                    "This prayer has been saved to your reflections. You can add your personal thoughts there.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Error saving reflection: {ex.Message}");
        }
    }
}
