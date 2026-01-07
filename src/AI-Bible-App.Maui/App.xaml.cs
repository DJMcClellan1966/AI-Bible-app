using Microsoft.Extensions.DependencyInjection;
using AI_Bible_App.Core.Interfaces;

namespace AI_Bible_App.Maui;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var shell = new AppShell();
		// Start with initialization page
		shell.CurrentItem = shell.Items[0]; // InitializationPage
		return new Window(shell);
	}
}