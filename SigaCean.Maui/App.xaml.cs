using Microsoft.Extensions.DependencyInjection;
using SigaCean.Presentation.Views;

namespace SigaCean;

public partial class App : Application
{
	private readonly IServiceProvider _serviceProvider;

	public App(IServiceProvider serviceProvider)
	{
		InitializeComponent();
		_serviceProvider = serviceProvider;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
		var window = new Window(new NavigationPage(loginPage));

		// Barra de titulo nativa na mesma cor da marca (teal do protótipo web).
		window.TitleBar = new TitleBar
		{
			Title = "SIGA-CEAN",
			Icon = "cean_logo.png",
			BackgroundColor = Color.FromArgb("#075F5B"),
			ForegroundColor = Colors.White
		};

		return window;
	}
}
