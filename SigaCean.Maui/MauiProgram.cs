using Microsoft.Extensions.Logging;
using SigaCean.Services;
using SigaCean.Infrastructure;
using SigaCean.Presentation.Views;

namespace SigaCean;

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

#if WINDOWS
		// No Windows o Title do Picker vira um cabecalho solto acima do campo, e o campo clicavel fica vazio e sem borda.
		// Aqui o Title passa a ser o texto de dentro do proprio campo, entao clicar nele abre a lista.
		Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("TituloDentroDoCampo", (handler, picker) =>
		{
			// Header e HeaderTemplate precisam ficar nulos, senao o Windows reserva a linha do cabecalho acima do campo.
			handler.PlatformView.Header = null;
			handler.PlatformView.HeaderTemplate = null;
			handler.PlatformView.Loaded += (campo, _) =>
			{
				var combo = (Microsoft.UI.Xaml.Controls.ComboBox)campo;
				combo.Header = null;
				combo.HeaderTemplate = null;
			};
			handler.PlatformView.PlaceholderText = picker.Title;
			handler.PlatformView.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
			handler.PlatformView.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;
			handler.PlatformView.MinHeight = 44;
			handler.PlatformView.Padding = new Microsoft.UI.Xaml.Thickness(12, 8, 12, 8);
			handler.PlatformView.VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
			handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(1);
			handler.PlatformView.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0xB5, 0xD3, 0xD0));
			handler.PlatformView.CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8);
		});

		// Campos de texto com a mesma borda arredondada dos seletores (o padrao do Windows nao mostra a caixa do campo).
		static void EstilizarCampo(Microsoft.UI.Xaml.Controls.TextBox campo)
		{
			campo.BorderThickness = new Microsoft.UI.Xaml.Thickness(1);
			campo.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0xB5, 0xD3, 0xD0));
			campo.CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8);
			campo.MinHeight = 44;
			campo.VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
		}

		Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("BordaDoCampo", (handler, _) => EstilizarCampo(handler.PlatformView));
		Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("BordaDoCampo", (handler, _) => EstilizarCampo(handler.PlatformView));
#endif

#if DEBUG
		builder.Logging.AddDebug();
#endif

		builder.Services.AddSingleton<DataStore>();
		builder.Services.AddSingleton<UsuarioService>();
		builder.Services.AddSingleton<ProdutoService>();
		builder.Services.AddSingleton<MovimentacaoService>();
		builder.Services.AddSingleton<EstoqueService>();
		builder.Services.AddSingleton<RelatorioService>();
		builder.Services.AddSingleton<AvisoService>();

		builder.Services.AddTransient<AppShell>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<PainelPage>();
		builder.Services.AddTransient<EstoquePage>();
		builder.Services.AddTransient<RegistrarEntradaPage>();
		builder.Services.AddTransient<RegistrarSaidaPage>();
		builder.Services.AddTransient<CadastrarProdutoPage>();
		builder.Services.AddTransient<MovimentacoesPage>();
		builder.Services.AddTransient<AjusteBalancoPage>();
		builder.Services.AddTransient<RelatorioConsumoPage>();
		builder.Services.AddTransient<CorrigirCancelarMovimentacaoPage>();
		builder.Services.AddTransient<ProdutoDetalhePage>();

		return builder.Build();
	}
}
