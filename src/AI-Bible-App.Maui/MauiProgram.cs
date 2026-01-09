using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Infrastructure.Repositories;
using AI_Bible_App.Infrastructure.Services;
using AI_Bible_App.Maui.Services;
using AI_Bible_App.Maui.ViewModels;
using AI_Bible_App.Maui.Views;
using CommunityToolkit.Maui;
using System.Reflection;

namespace AI_Bible_App.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Add Configuration
		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream("AI_Bible_App.Maui.appsettings.json");
		if (stream != null)
		{
			var config = new ConfigurationBuilder()
				.AddJsonStream(stream)
				.Build();
			builder.Configuration.AddConfiguration(config);
		}
		builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

		// Core services
		builder.Services.AddSingleton<INavigationService, NavigationService>();
		builder.Services.AddSingleton<ICharacterRepository, InMemoryCharacterRepository>();
		builder.Services.AddSingleton<IChatRepository, JsonChatRepository>();
		builder.Services.AddSingleton<IPrayerRepository, JsonPrayerRepository>();
		builder.Services.AddSingleton<IReflectionRepository, JsonReflectionRepository>();
		builder.Services.AddSingleton<IHealthCheckService, HealthCheckService>();
		builder.Services.AddSingleton<IBibleLookupService, BibleLookupService>();
		
		// AI Services - Hybrid (local Ollama + cloud Groq fallback)
		builder.Services.AddSingleton<LocalAIService>();
		builder.Services.AddSingleton<GroqAIService>();
		builder.Services.AddSingleton<IAIService, HybridAIService>();

		// Register ViewModels
		builder.Services.AddTransient<CharacterSelectionViewModel>();
		builder.Services.AddTransient<ChatViewModel>();
		builder.Services.AddTransient<ChatHistoryViewModel>();
		builder.Services.AddTransient<PrayerViewModel>();
		builder.Services.AddTransient<ReflectionViewModel>();

		// Register Pages
		builder.Services.AddTransient<CharacterSelectionPage>();
		builder.Services.AddTransient<ChatPage>();
		builder.Services.AddTransient<ChatHistoryPage>();
		builder.Services.AddTransient<PrayerPage>();
		builder.Services.AddTransient<ReflectionPage>();

		return builder.Build();
	}
}
