using AI_Bible_App.Maui.Views;

namespace AI_Bible_App.Maui;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register routes for navigation
		Routing.RegisterRoute("chat", typeof(ChatPage));
		Routing.RegisterRoute("prayer", typeof(PrayerPage));
	}
}
