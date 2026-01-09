using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using AI_Bible_App.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

#pragma warning disable MVVMTK0045 // AOT compatibility warning for WinRT scenarios

namespace AI_Bible_App.Maui.ViewModels;

public partial class CharacterSelectionViewModel : BaseViewModel
{
    private readonly ICharacterRepository _characterRepository;
    private readonly INavigationService _navigationService;
    private readonly IHealthCheckService? _healthCheckService;

    [ObservableProperty]
    private ObservableCollection<BiblicalCharacter> characters = new();

    [ObservableProperty]
    private BiblicalCharacter? selectedCharacter;

    [ObservableProperty]
    private bool isOllamaOnline;

    [ObservableProperty]
    private string ollamaStatusText = "Checking AI status...";

    [ObservableProperty]
    private Color ollamaStatusColor = Colors.Gray;

    public CharacterSelectionViewModel(
        ICharacterRepository characterRepository,
        INavigationService navigationService,
        IHealthCheckService? healthCheckService = null)
    {
        _characterRepository = characterRepository;
        _navigationService = navigationService;
        _healthCheckService = healthCheckService;
        Title = "Choose a Biblical Character";
    }

    public async Task InitializeAsync()
    {
        await LoadCharactersAsync();
        await CheckOllamaStatusAsync();
    }

    private async Task LoadCharactersAsync()
    {
        try
        {
            IsBusy = true;
            var chars = await _characterRepository.GetAllCharactersAsync();
            Characters = new ObservableCollection<BiblicalCharacter>(chars);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CheckOllamaStatusAsync()
    {
        if (_healthCheckService == null)
        {
            IsOllamaOnline = false;
            OllamaStatusText = "⚠️ Health check unavailable";
            OllamaStatusColor = Colors.Orange;
            return;
        }

        try
        {
            OllamaStatusText = "🔄 Checking AI...";
            OllamaStatusColor = Colors.Gray;
            
            var isAvailable = await _healthCheckService.IsOllamaAvailableAsync();
            IsOllamaOnline = isAvailable;
            
            if (isAvailable)
            {
                OllamaStatusText = "✅ AI Ready (Ollama Online)";
                OllamaStatusColor = Colors.Green;
            }
            else
            {
                OllamaStatusText = "❌ AI Offline - Start Ollama";
                OllamaStatusColor = Colors.Red;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Ollama check failed: {ex.Message}");
            IsOllamaOnline = false;
            OllamaStatusText = "❌ AI Offline - Start Ollama";
            OllamaStatusColor = Colors.Red;
        }
    }

    [RelayCommand]
    private async Task SelectCharacter(BiblicalCharacter character)
    {
        System.Diagnostics.Debug.WriteLine($"[DEBUG] SelectCharacter called with: {character?.Name}");
        
        if (character == null || IsBusy)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] SelectCharacter early exit - character null: {character == null}, IsBusy: {IsBusy}");
            return;
        }

        try
        {
            SelectedCharacter = character;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] About to navigate to chat page...");
            await _navigationService.NavigateToAsync("chat", new Dictionary<string, object>
            {
                { "character", character }
            });
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Navigation completed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Navigation FAILED: {ex}");
            throw;
        }
    }

    [RelayCommand]
    private async Task NavigateToPrayer()
    {
        await _navigationService.NavigateToAsync("prayer");
    }
}
