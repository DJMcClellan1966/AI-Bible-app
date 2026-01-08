using AI_Bible_App.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

#pragma warning disable MVVMTK0045 // AOT compatibility warning for WinRT scenarios

namespace AI_Bible_App.Maui.ViewModels;

public partial class InitializationViewModel : BaseViewModel
{
    private readonly IBibleRAGService _ragService;
    private readonly IHealthCheckService _healthCheckService;

    [ObservableProperty]
    private string statusMessage = "Initializing...";

    [ObservableProperty]
    private double progress = 0;

    [ObservableProperty]
    private bool isInitializing = true;

    [ObservableProperty]
    private bool hasError = false;

    [ObservableProperty]
    private string? errorMessage;

    public InitializationViewModel(IBibleRAGService ragService, IHealthCheckService healthCheckService)
    {
        _ragService = ragService;
        _healthCheckService = healthCheckService;
        Title = "Voices of Scripture";
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Check Ollama health
            StatusMessage = "Checking Ollama service...";
            Progress = 0.1;

            var health = await _healthCheckService.GetHealthStatusAsync();
            if (!health.IsHealthy)
            {
                HasError = true;
                ErrorMessage = health.ErrorMessage;
                IsInitializing = false;
                return;
            }

            Progress = 0.3;

            // Initialize RAG if not already done
            if (!_ragService.IsInitialized)
            {
                StatusMessage = "Loading Bible verses...";
                Progress = 0.5;

                await _ragService.InitializeAsync();

                StatusMessage = "Indexing Scripture...";
                Progress = 0.9;
            }

            Progress = 1.0;
            StatusMessage = "Ready!";
            IsInitializing = false;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Initialization failed: {ex.Message}";
            IsInitializing = false;
        }
    }
}
