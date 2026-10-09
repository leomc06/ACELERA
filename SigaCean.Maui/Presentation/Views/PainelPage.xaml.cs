using Microsoft.Extensions.DependencyInjection;
using SigaCean.Domain;
using SigaCean.Presentation.ViewModels;
using SigaCean.Services;

namespace SigaCean.Presentation.Views;

// Tela inicial: so conta e lista o que os Services ja calculam (nenhuma regra de estoque aqui).
public partial class PainelPage : ContentPage
{
    private const int MovimentacoesExibidas = 8;

    private readonly EstoqueService _estoqueService;
    private readonly MovimentacaoService _movimentacaoService;
    private readonly UsuarioService _usuarioService;
    private readonly AvisoService _avisoService;
    private readonly IServiceProvider _serviceProvider;

    public PainelPage(EstoqueService estoqueService, MovimentacaoService movimentacaoService,
        UsuarioService usuarioService, AvisoService avisoService, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _estoqueService = estoqueService;
        _movimentacaoService = movimentacaoService;
        _usuarioService = usuarioService;
        _avisoService = avisoService;
        _serviceProvider = serviceProvider;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var usuario = _usuarioService.UsuarioLogado;
        CabecalhoControl.Atualizar(usuario);
        AcoesAdministrativoLayout.IsVisible = usuario?.Perfil == Perfil.Administrativo;
        AvisoBannerControl.Mostrar(_avisoService.Consumir());

        DesenharIndicadores();

        var movimentacoes = _movimentacaoService.ObterHistorico()
            .Take(MovimentacoesExibidas)
            .Select(m => new MovimentacaoHistoricoItem { Movimentacao = m })
            .ToList();
        BindableLayout.SetItemsSource(MovimentacoesLayout, movimentacoes);
        SemMovimentacoesLabel.IsVisible = movimentacoes.Count == 0;
    }

    private void DesenharIndicadores()
    {
        IndicadoresLayout.Children.Clear();
        AdicionarIndicador("Sem estoque", SituacaoEstoque.SemEstoque, "#D95757");
        AdicionarIndicador("Abaixo do mínimo", SituacaoEstoque.AbaixoDoMinimo, "#D95757");
        AdicionarIndicador("Vencidos", SituacaoEstoque.Vencido, "#D95757");
        AdicionarIndicador("Validade próxima", SituacaoEstoque.ValidadeProxima, "#F59B24");
    }

    private void AdicionarIndicador(string titulo, SituacaoEstoque situacao, string corZerado)
    {
        var quantidade = _estoqueService.Contar(situacao);
        var cor = quantidade == 0 ? Color.FromArgb("#169B62") : Color.FromArgb(corZerado);

        var quadro = new Border
        {
            Margin = new Thickness(6),
            Padding = new Thickness(18, 14),
            WidthRequest = 190,
            Content = new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Label { Text = quantidade.ToString(), FontSize = 34, FontAttributes = FontAttributes.Bold, TextColor = cor },
                    new Label { Text = titulo, FontAttributes = FontAttributes.Bold },
                    new Label { Text = quantidade == 0 ? "nenhum produto" : quantidade == 1 ? "produto" : "produtos", FontSize = 12, TextColor = Color.FromArgb("#718589") }
                }
            }
        };
        SemanticProperties.SetDescription(quadro, $"{titulo}: {quantidade} produtos. Toque para ver a lista.");

        var toque = new TapGestureRecognizer();
        toque.Tapped += (_, _) => (Shell.Current as AppShell)?.IrParaEstoque(situacao);
        quadro.GestureRecognizers.Add(toque);

        IndicadoresLayout.Children.Add(quadro);
    }

    private void OnVerTodasClicked(object? sender, EventArgs e) => (Shell.Current as AppShell)?.IrParaMovimentacoes();

    private async void OnEntradaClicked(object? sender, EventArgs e) =>
        await Navigation.PushAsync(_serviceProvider.GetRequiredService<RegistrarEntradaPage>());

    private async void OnSaidaClicked(object? sender, EventArgs e) =>
        await Navigation.PushAsync(_serviceProvider.GetRequiredService<RegistrarSaidaPage>());
}
