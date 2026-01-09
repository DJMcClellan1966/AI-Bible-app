using AI_Bible_App.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

#pragma warning disable MVVMTK0045

namespace AI_Bible_App.Maui.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ITrainingDataExporter _exporter;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private int totalSessions;

    [ObservableProperty]
    private int totalMessages;

    [ObservableProperty]
    private int ratedMessages;

    [ObservableProperty]
    private int positiveRatings;

    [ObservableProperty]
    private int negativeRatings;

    [ObservableProperty]
    private int messagesWithFeedback;

    public SettingsViewModel(ITrainingDataExporter exporter, IDialogService dialogService)
    {
        _exporter = exporter;
        _dialogService = dialogService;
        Title = "Settings";
    }

    public async Task InitializeAsync()
    {
        await RefreshStatsAsync();
    }

    [RelayCommand]
    private async Task RefreshStatsAsync()
    {
        try
        {
            IsBusy = true;
            var stats = await _exporter.GetStatsAsync();
            
            TotalSessions = stats.TotalSessions;
            TotalMessages = stats.TotalMessages;
            RatedMessages = stats.RatedMessages;
            PositiveRatings = stats.PositiveRatings;
            NegativeRatings = stats.NegativeRatings;
            MessagesWithFeedback = stats.MessagesWithFeedback;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportAllRatedAsync()
    {
        try
        {
            IsBusy = true;
            
            if (RatedMessages == 0)
            {
                await _dialogService.ShowAlertAsync("No Data", "No rated messages to export. Rate some AI responses first!");
                return;
            }

            var filePath = await _exporter.ExportToJsonlAsync(onlyPositiveRatings: false);
            
            await _dialogService.ShowAlertAsync(
                "Export Complete",
                $"Exported {RatedMessages} rated conversations to:\n{filePath}");
                
            // Optionally share the file
            await ShareFileAsync(filePath);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Export Failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportPositiveOnlyAsync()
    {
        try
        {
            IsBusy = true;
            
            if (PositiveRatings == 0)
            {
                await _dialogService.ShowAlertAsync("No Data", "No positive ratings to export. Give some thumbs up first!");
                return;
            }

            var filePath = await _exporter.ExportToJsonlAsync(onlyPositiveRatings: true);
            
            await _dialogService.ShowAlertAsync(
                "Export Complete",
                $"Exported {PositiveRatings} positive-rated conversations to:\n{filePath}");
                
            await ShareFileAsync(filePath);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Export Failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ShareFileAsync(string filePath)
    {
        try
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Export Training Data",
                File = new ShareFile(filePath)
            });
        }
        catch
        {
            // Sharing not available on all platforms
        }
    }
}
