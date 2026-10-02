using Microsoft.Extensions.DependencyInjection;
using SigaCean.Services;

namespace SigaCean.Presentation.Views;

public partial class LoginPage : ContentPage
{
	private readonly UsuarioService _usuarioService;
	private readonly IServiceProvider _serviceProvider;

	public LoginPage(UsuarioService usuarioService, IServiceProvider serviceProvider)
	{
		InitializeComponent();
		_usuarioService = usuarioService;
		_serviceProvider = serviceProvider;
	}

	private void OnEntrarClicked(object? sender, EventArgs e)
	{
		var usuario = _usuarioService.Login(LoginEntry.Text ?? string.Empty, SenhaEntry.Text ?? string.Empty);

		if (usuario is null)
		{
			MensagemErroLabel.Text = "Usuário ou senha inválidos.";
			MensagemErroLabel.IsVisible = true;
			return;
		}

		var appShell = _serviceProvider.GetRequiredService<AppShell>();
		Application.Current!.Windows[0].Page = appShell;
	}
}
