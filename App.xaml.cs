using Microsoft.Extensions.DependencyInjection;
namespace BlockDrop;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell())
		{
			Title = "BlockDrop Pocket",
#if WINDOWS
			Width = 430,
			Height = 820,
			MinimumWidth = 360,
			MinimumHeight = 600,
#endif
		};
	}
}
