using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using AI_Bible_App.Infrastructure.Repositories;
using AI_Bible_App.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI_Bible_App.Console;

class Program
{
    static async Task Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.local.json", optional: true)
            .Build();

        // Setup dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services, configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Run the application
        var app = new BibleApp(
            serviceProvider.GetRequiredService<IAIService>(),
            serviceProvider.GetRequiredService<ICharacterRepository>(),
            serviceProvider.GetRequiredService<IChatRepository>(),
            serviceProvider.GetRequiredService<IPrayerRepository>(),
            serviceProvider.GetRequiredService<ILogger<BibleApp>>()
        );

        await app.RunAsync();
    }

    static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Register services
        services.AddSingleton<IAIService, OpenAIService>();
        services.AddSingleton<ICharacterRepository, InMemoryCharacterRepository>();
        services.AddSingleton<IChatRepository, JsonChatRepository>();
        services.AddSingleton<IPrayerRepository, JsonPrayerRepository>();
        services.AddSingleton<BibleApp>();
    }
}
