using Microsoft.Extensions.DependencyInjection;
using SigaCean.Domain;
using SigaCean.Services;
using SigaCean.Presentation.Views;

namespace SigaCean;

public partial class AppShell : Shell
{
    // Itens do menu lateral espelham a navegacao principal do protótipo web
    // (sidebar: Estoque, Movimentações, Relatórios) — sem inventar secoes que o app nao tem.
    private readonly IServiceProvider _serviceProvider;
    private readonly UsuarioService _usuarioService;
    private readonly FlyoutItem _estoqueItem;
    private readonly FlyoutItem _movimentacoesItem;
    private readonly EstoquePage _estoquePage;

    public AppShell(IServiceProvider serviceProvider, UsuarioService usuarioService)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _usuarioService = usuarioService;

        Items.Add(CriarItem("⌂  Painel", serviceProvider.GetRequiredService<PainelPage>()));

        _estoquePage = serviceProvider.GetRequiredService<EstoquePage>();
        _estoqueItem = CriarItem("▣  Estoque", _estoquePage);
        Items.Add(_estoqueItem);

        _movimentacoesItem = CriarItem("⇄  Movimentações", serviceProvider.GetRequiredService<MovimentacoesPage>());
        Items.Add(_movimentacoesItem);

        // UC08 (CONFIRMADO): Gerar Relatorio de Consumo e uma acao exclusiva do Administrativo.
        if (usuarioService.UsuarioLogado?.Perfil == Perfil.Administrativo)
        {
            Items.Add(CriarItem("▥  Relatório", serviceProvider.GetRequiredService<RelatorioConsumoPage>()));
            Items.Add(CriarItem("▤  Gráficos", serviceProvider.GetRequiredService<GraficosPage>()));
            Items.Add(CriarItem("▦  Rel. por categoria", serviceProvider.GetRequiredService<RelatorioCategoriaPage>()));
        }
    }

    // Usados pelo Painel: abre o Estoque ja filtrado, ou o historico completo.
    public void IrParaEstoque(SituacaoEstoque situacao)
    {
        _estoquePage.AplicarSituacao(situacao);
        CurrentItem = _estoqueItem;
    }

    public void IrParaMovimentacoes() => CurrentItem = _movimentacoesItem;

    // Volta para o login para trocar de usuario. Os dados em memoria continuam; so a sessao termina.
    // Um novo AppShell e criado no proximo login, pois o menu depende do perfil de quem entrou.
    private async void OnSairClicked(object? sender, EventArgs e)
    {
        var confirmou = await DisplayAlertAsync("Sair", $"Encerrar a sessão de {_usuarioService.UsuarioLogado?.Nome}?", "Sair", "Continuar");
        if (!confirmou) return;

        _usuarioService.Logout();
        var login = _serviceProvider.GetRequiredService<LoginPage>();
        Application.Current!.Windows[0].Page = new NavigationPage(login);
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
