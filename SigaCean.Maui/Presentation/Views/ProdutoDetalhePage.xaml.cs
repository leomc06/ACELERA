using Microsoft.Extensions.DependencyInjection;
using SigaCean.Domain;
using SigaCean.Presentation.ViewModels;
using SigaCean.Services;

namespace SigaCean.Presentation.Views;

// Ficha de consulta de um produto: situacao, saldos por validade (na ordem FEFO) e as ultimas movimentacoes.
// Os dois perfis consultam; as acoes (entrada, saida, editar) so aparecem para o Administrativo.
public partial class ProdutoDetalhePage : ContentPage
{
    private const int MovimentacoesExibidas = 10;

    private readonly ProdutoService _produtoService;
    private readonly MovimentacaoService _movimentacaoService;
    private readonly UsuarioService _usuarioService;
    private readonly IServiceProvider _serviceProvider;
    private Guid? _produtoId;

    public ProdutoDetalhePage(ProdutoService produtoService, MovimentacaoService movimentacaoService,
        UsuarioService usuarioService, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _produtoService = produtoService;
        _movimentacaoService = movimentacaoService;
        _usuarioService = usuarioService;
        _serviceProvider = serviceProvider;
    }

    public void Carregar(Guid produtoId)
    {
        _produtoId = produtoId;
        Atualizar();
    }

    // Tambem ao voltar de uma entrada, saida ou edicao: a ficha reflete o estoque novo.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        Atualizar();
    }

    private void Atualizar()
    {
        if (_produtoId is not { } id) return;

        var produto = _produtoService.ObterPorId(id);
        var item = new ProdutoEstoqueItem { Produto = produto };

        NomeLabel.Text = produto.Nome;
        CategoriaLabel.Text = item.Categoria;
        AcoesAdministrativoLayout.IsVisible = _usuarioService.UsuarioLogado?.Perfil == Perfil.Administrativo;

        QuantidadeLabel.Text = item.QuantidadeAtual.ToString();
        QuantidadeLabel.TextColor = item.CorSaude;
        MinimoLabel.Text = item.MostrarMinimo
            ? $"Estoque mínimo: {produto.EstoqueMinimo} ({item.PercentualTexto})"
            : "Estoque mínimo não definido.";

        AlertasLayout.Children.Clear();
        if (item.MostrarAlertaSemEstoque) AdicionarAlerta("SEM ESTOQUE", Color.FromArgb("#D95757"));
        if (item.MostrarAlertaCritico) AdicionarAlerta("Estoque crítico", Color.FromArgb("#D95757"));
        else if (produto.EstaAbaixoDoMinimo && !item.MostrarAlertaSemEstoque) AdicionarAlerta("Abaixo do mínimo", Color.FromArgb("#D95757"));
        if (item.MostrarAlertaVencido) AdicionarAlerta("VENCIDO", Color.FromArgb("#D95757"));
        if (item.MostrarAlertaValidadeProxima) AdicionarAlerta("Validade próxima", Color.FromArgb("#F59B24"));
        AlertasLayout.IsVisible = AlertasLayout.Children.Count > 0;

        var saldos = produto.Lotes
            .Where(l => l.Quantidade > 0)
            .OrderBy(l => l.DataValidade ?? DateOnly.MaxValue)
            .Select((l, i) => new SaldoValidadeItem { Saldo = l, Ordem = i + 1 })
            .ToList();
        BindableLayout.SetItemsSource(SaldosLayout, saldos);
        SemSaldosLabel.IsVisible = saldos.Count == 0;

        var movimentacoes = _movimentacaoService.ObterHistorico(produto)
            .Take(MovimentacoesExibidas)
            .Select(m => new MovimentacaoHistoricoItem { Movimentacao = m })
            .ToList();
        BindableLayout.SetItemsSource(MovimentacoesLayout, movimentacoes);
        SemMovimentacoesLabel.IsVisible = movimentacoes.Count == 0;
    }

    private void AdicionarAlerta(string texto, Color cor) =>
        AlertasLayout.Children.Add(new Label
        {
            Text = texto, TextColor = cor, FontAttributes = FontAttributes.Bold, FontSize = 13, Margin = new Thickness(0, 0, 12, 0)
        });

    private async void OnEntradaClicked(object? sender, EventArgs e)
    {
        if (_produtoId is not { } id) return;
        var pagina = _serviceProvider.GetRequiredService<RegistrarEntradaPage>();
        pagina.PreSelecionar(id);
        await Navigation.PushAsync(pagina);
    }

    private async void OnSaidaClicked(object? sender, EventArgs e)
    {
        if (_produtoId is not { } id) return;
        var pagina = _serviceProvider.GetRequiredService<RegistrarSaidaPage>();
        pagina.PreSelecionar(id);
        await Navigation.PushAsync(pagina);
    }

    private async void OnEditarClicked(object? sender, EventArgs e)
    {
        if (_produtoId is not { } id) return;
        var pagina = _serviceProvider.GetRequiredService<CadastrarProdutoPage>();
        pagina.CarregarParaEdicao(id);
        await Navigation.PushAsync(pagina);
    }
}
