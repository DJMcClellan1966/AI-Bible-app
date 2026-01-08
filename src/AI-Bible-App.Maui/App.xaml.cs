using Microsoft.Extensions.DependencyInjection;
using AI_Bible_App.Core.Interfaces;
using System.Runtime.ExceptionServices;

namespace AI_Bible_App.Maui;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}