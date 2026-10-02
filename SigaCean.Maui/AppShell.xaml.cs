using Microsoft.Extensions.DependencyInjection;
using SigaCean.Domain;
using SigaCean.Services;
using SigaCean.Presentation.Views;

namespace SigaCean;

public partial class AppShell : Shell
{
    // Itens do menu lateral espelham a navegacao principal do protótipo web
    // (sidebar: Estoque, Movimentações, Relatórios) — sem inventar secoes que o app nao tem.
    public AppShell(IServiceProvider serviceProvider, UsuarioService usuarioService)
    {
        InitializeComponent();

        Items.Add(CriarItem("▣  Estoque", serviceProvider.GetRequiredService<EstoquePage>()));
        Items.Add(CriarItem("⇄  Movimentações", serviceProvider.GetRequiredService<MovimentacoesPage>()));

        // UC08 (CONFIRMADO): Gerar Relatorio de Consumo e uma acao exclusiva do Administrativo.
        if (usuarioService.UsuarioLogado?.Perfil == Perfil.Administrativo)
        {
            Items.Add(CriarItem("▥  Relatório", serviceProvider.GetRequiredService<RelatorioConsumoPage>()));
        }
    }

    private static FlyoutItem CriarItem(string titulo, ContentPage pagina)
    {
        pagina.Title = titulo;
        return new FlyoutItem
        {
            Title = titulo,
            Items = { new ShellContent { Title = titulo, Content = pagina } }
        };
    }
}
