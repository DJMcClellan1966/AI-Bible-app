using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Infrastructure.Repositories;
using AI_Bible_App.Infrastructure.Services;
using AI_Bible_App.Maui.Services;
using AI_Bible_App.Maui.ViewModels;
using AI_Bible_App.Maui.Views;
using System.Reflection;

namespace AI_Bible_App.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
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
		builder.Services.AddSingleton(builder.Configuration);

		// Register services
		builder.Services.AddSingleton<INavigationService, NavigationService>();
		builder.Services.AddSingleton<IHealthCheckService, HealthCheckService>();
		builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
		builder.Services.AddSingleton<IFileSecurityService, FileSecurityService>();
		
		// Use WEB Bible repository by default
		builder.Services.AddSingleton<IBibleRepository, WebBibleRepository>();
		builder.Services.AddSingleton<IBibleRAGService, BibleRAGService>();
		builder.Services.AddSingleton<IAIService, LocalAIService>();
		builder.Services.AddSingleton<ICharacterRepository, InMemoryCharacterRepository>();
		builder.Services.AddSingleton<IChatRepository, JsonChatRepository>();
		builder.Services.AddSingleton<IPrayerRepository, JsonPrayerRepository>();

		// Register ViewModels
		builder.Services.AddTransient<InitializationViewModel>();
		builder.Services.AddTransient<CharacterSelectionViewModel>();
		builder.Services.AddTransient<ChatViewModel>();
		builder.Services.AddTransient<PrayerViewModel>();

		// Register Pages
		builder.Services.AddTransient<InitializationPage>();
		builder.Services.AddTransient<CharacterSelectionPage>();
		builder.Services.AddTransient<ChatPage>();
		builder.Services.AddTransient<PrayerPage>();

		return builder.Build();
	}
}
