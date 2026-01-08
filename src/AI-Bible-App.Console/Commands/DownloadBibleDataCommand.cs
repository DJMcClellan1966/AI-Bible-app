using AI_Bible_App.Infrastructure.Utilities;
using Microsoft.Extensions.Logging;

namespace AI_Bible_App.Console.Commands;

/// <summary>
/// Console command to download full Bible data
/// </summary>
public class DownloadBibleDataCommand
{
    private readonly ILogger<DownloadBibleDataCommand> _logger;

    public DownloadBibleDataCommand(ILogger<DownloadBibleDataCommand> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        System.Console.WriteLine("=== Bible Data Downloader ===");
        System.Console.WriteLine("This will download full Bible text from public domain sources.");
        System.Console.WriteLine();

        var downloader = new BibleDataDownloader(LoggerFactory.Create(b => b.AddConsole()).CreateLogger<BibleDataDownloader>());

        // Determine output directory
        var baseDir = AppContext.BaseDirectory;
        var dataDir = Path.Combine(baseDir, "..", "..", "..", "..", "AI-Bible-App.Maui", "Data", "Bible");
        var fullDataDir = Path.GetFullPath(dataDir);

        System.Console.WriteLine($"Output directory: {fullDataDir}");
        System.Console.WriteLine();

        // Download WEB
        System.Console.WriteLine("Downloading World English Bible (WEB)...");
        try
        {
            var webVerses = await downloader.DownloadWebBibleAsync();
            var webPath = Path.Combine(fullDataDir, "web.json");
            await downloader.SaveToFileAsync(webVerses, webPath);
            System.Console.WriteLine($"✓ WEB Bible saved: {webVerses.Count} verses");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download WEB Bible");
            System.Console.WriteLine($"✗ WEB download failed: {ex.Message}");
        }

        System.Console.WriteLine();

        // Download KJV
        System.Console.WriteLine("Downloading King James Version (KJV)...");
        try
        {
            var kjvVerses = await downloader.DownloadKjvBibleAsync();
            var kjvPath = Path.Combine(fullDataDir, "kjv.json");
            await downloader.SaveToFileAsync(kjvVerses, kjvPath);
            System.Console.WriteLine($"✓ KJV Bible saved: {kjvVerses.Count} verses");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download KJV Bible");
            System.Console.WriteLine($"✗ KJV download failed: {ex.Message}");
        }

        System.Console.WriteLine();
        System.Console.WriteLine("Download complete! You can now run the MAUI app.");
    }
}
