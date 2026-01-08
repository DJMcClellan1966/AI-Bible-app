using AI_Bible_App.Maui.Views;

namespace AI_Bible_App.Maui;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register chat route
		Routing.RegisterRoute("chat", typeof(ChatPage));
	}
}
