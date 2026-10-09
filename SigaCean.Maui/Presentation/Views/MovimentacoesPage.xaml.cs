using Microsoft.Extensions.DependencyInjection;
using SigaCean.Domain;
using SigaCean.Services;
using SigaCean.Presentation.ViewModels;

namespace SigaCean.Presentation.Views;

public partial class MovimentacoesPage : ContentPage
{
    private readonly MovimentacaoService _movimentacaoService;
    private readonly UsuarioService _usuarioService;
    private readonly IServiceProvider _serviceProvider;
    private List<MovimentacaoHistoricoItem> _todosOsItens = new();
    private bool _carregandoFiltros;

    // Desenhar centenas de linhas de uma vez deixa a tela lenta; mostra-se um bloco e "Mostrar mais" traz o proximo.
    private const int TamanhoDoBloco = 100;
    private int _limiteExibido = TamanhoDoBloco;

    private static readonly (TipoMovimentacao? Valor, string Texto)[] Tipos =
    [
        (null, "Todos os tipos"),
        (TipoMovimentacao.Entrada, "Entradas"),
        (TipoMovimentacao.Saida, "Saídas"),
        (TipoMovimentacao.Ajuste, "Ajustes de balanço")
    ];

    private static readonly (StatusMovimentacao? Valor, string Texto)[] Situacoes =
    [
        (null, "Todas as situações"),
        (StatusMovimentacao.Ativa, "Ativas"),
        (StatusMovimentacao.Corrigida, "Corrigidas"),
        (StatusMovimentacao.Cancelada, "Canceladas")
    ];

    private readonly AvisoService _avisoService;

    public MovimentacoesPage(MovimentacaoService movimentacaoService, UsuarioService usuarioService, IServiceProvider serviceProvider, AvisoService avisoService)
    {
        InitializeComponent();
        _movimentacaoService = movimentacaoService;
        _usuarioService = usuarioService;
        _serviceProvider = serviceProvider;
        _avisoService = avisoService;

        _carregandoFiltros = true;
        TipoFiltroPicker.ItemsSource = Tipos.Select(t => t.Texto).ToList();
        TipoFiltroPicker.SelectedIndex = 0;
        StatusFiltroPicker.ItemsSource = Situacoes.Select(s => s.Texto).ToList();
        StatusFiltroPicker.SelectedIndex = 0;
        DataInicioPicker.Date = DateTime.Today.AddDays(-30);
        DataFimPicker.Date = DateTime.Today;
        _carregandoFiltros = false;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CarregarHistorico();
        PreencherUsuarioLogado();
        AvisoBannerControl.Mostrar(_avisoService.Consumir());
    }

    private void PreencherUsuarioLogado()
    {
        var usuario = _usuarioService.UsuarioLogado;
        AcoesAdministrativoLayout.IsVisible = usuario?.Perfil == Perfil.Administrativo;
        CabecalhoControl.Atualizar(usuario);
    }

    private void CarregarHistorico()
    {
        DateTime? inicio = null, fim = null;
        if (FiltrarPeriodoSwitch.IsToggled)
        {
            var de = (DataInicioPicker.Date ?? DateTime.Today).Date;
            var ate = (DataFimPicker.Date ?? DateTime.Today).Date;
            if (de > ate)
            {
                MostrarMensagem("A data inicial não pode ser depois da data final.");
                return; // mantem a lista anterior ate o usuario corrigir as datas
            }
            (inicio, fim) = (de, ate.AddDays(1).AddTicks(-1)); // inclui o dia final por inteiro
        }
        MostrarMensagem(null);

        var tipo = Tipos[Math.Max(TipoFiltroPicker.SelectedIndex, 0)].Valor;
        var status = Situacoes[Math.Max(StatusFiltroPicker.SelectedIndex, 0)].Valor;

        _limiteExibido = TamanhoDoBloco;
        _todosOsItens = _movimentacaoService.ObterHistorico(null, inicio, fim, tipo, status)
            .Select(m => new MovimentacaoHistoricoItem { Movimentacao = m })
            .ToList();

        AplicarPesquisa();
    }

    private void OnPesquisaTextChanged(object? sender, TextChangedEventArgs e)
    {
        _limiteExibido = TamanhoDoBloco;
        AplicarPesquisa();
    }

    private void OnMostrarMaisClicked(object? sender, EventArgs e)
    {
        _limiteExibido += TamanhoDoBloco;
        AplicarPesquisa();
    }

    // Usa o texto que esta no campo, tambem ao recarregar a lista (ao voltar de outra tela), para os dois nao divergirem.
    private void AplicarPesquisa()
    {
        var termo = PesquisaEntry.Text?.Trim();
        var itens = string.IsNullOrEmpty(termo)
            ? _todosOsItens
            : _todosOsItens.Where(i =>
                BuscaTexto.Contem(i.ProdutoNome, termo) ||
                BuscaTexto.Contem(i.DetalheTexto, termo)).ToList(); // tambem acha por retirante, documento, destino

        MovimentacoesCollectionView.ItemsSource = itens.Take(_limiteExibido).ToList();
        MostrarMaisButton.IsVisible = itens.Count > _limiteExibido;
        MostrarMaisButton.Text = $"Mostrar mais ({itens.Count - _limiteExibido} restantes)";

        ResultadoLabel.Text = itens.Count switch
        {
            1 => "1 movimentação encontrada.",
            _ when itens.Count > _limiteExibido => $"{itens.Count} movimentações encontradas — mostrando as {_limiteExibido} mais recentes.",
            _ => $"{itens.Count} movimentações encontradas."
        };
    }

    // ---- Filtros de tipo, situacao e periodo ----

    private void OnFiltroChanged(object? sender, EventArgs e)
    {
        if (_carregandoFiltros) return;
        CarregarHistorico();
    }

    private void OnFiltrarPeriodoToggled(object? sender, ToggledEventArgs e)
    {
        PeriodoLayout.IsVisible = e.Value;
        CarregarHistorico();
    }

    private void OnPeriodoChanged(object? sender, DateChangedEventArgs e)
    {
        if (_carregandoFiltros) return;
        CarregarHistorico();
    }

    private void MostrarMensagem(string? texto)
    {
        MensagemLabel.Text = texto ?? string.Empty;
        MensagemLabel.IsVisible = !string.IsNullOrEmpty(texto);
    }

    private async void OnEntradaClicked(object? sender, EventArgs e)
    {
        var pagina = _serviceProvider.GetRequiredService<RegistrarEntradaPage>();
        await Navigation.PushAsync(pagina);
    }

    private async void OnSaidaClicked(object? sender, EventArgs e)
    {
        var pagina = _serviceProvider.GetRequiredService<RegistrarSaidaPage>();
        await Navigation.PushAsync(pagina);
    }

    // RD0018/RD0019 (CONFIRMADO): so o Administrativo corrige ou cancela movimentacao,
    // e so faz sentido agir sobre uma movimentacao que ainda esta Ativa.
    private async void OnMovimentacaoTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Border border || border.BindingContext is not MovimentacaoHistoricoItem item) return;
        if (_usuarioService.UsuarioLogado?.Perfil != Perfil.Administrativo) return;
        if (item.Movimentacao.Status != StatusMovimentacao.Ativa) return;

        var pagina = _serviceProvider.GetRequiredService<CorrigirCancelarMovimentacaoPage>();
        pagina.CarregarMovimentacao(item.Movimentacao);
        await Navigation.PushAsync(pagina);
    }
}
