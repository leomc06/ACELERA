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

    public MovimentacoesPage(MovimentacaoService movimentacaoService, UsuarioService usuarioService, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _movimentacaoService = movimentacaoService;
        _usuarioService = usuarioService;
        _serviceProvider = serviceProvider;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CarregarHistorico();
        PreencherUsuarioLogado();
    }

    private void PreencherUsuarioLogado()
    {
        var usuario = _usuarioService.UsuarioLogado;
        AcoesAdministrativoLayout.IsVisible = usuario?.Perfil == Perfil.Administrativo;
        UsuarioInicialLabel.Text = usuario is null ? "?" : usuario.Nome[..1].ToUpperInvariant();
        UsuarioNomeLabel.Text = usuario?.Nome ?? string.Empty;
        UsuarioPerfilLabel.Text = usuario?.Perfil switch
        {
            Perfil.Administrativo => "Administrativo",
            Perfil.Veterinario => "Veterinário",
            _ => string.Empty
        };
    }

    private void CarregarHistorico()
    {
        _todosOsItens = _movimentacaoService.ObterHistorico()
            .Select(m => new MovimentacaoHistoricoItem { Movimentacao = m })
            .ToList();

        MovimentacoesCollectionView.ItemsSource = _todosOsItens;
    }

    private void OnPesquisaTextChanged(object? sender, TextChangedEventArgs e)
    {
        var termo = e.NewTextValue;
        MovimentacoesCollectionView.ItemsSource = string.IsNullOrWhiteSpace(termo)
            ? _todosOsItens
            : _todosOsItens.Where(i =>
                i.ProdutoNome.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                i.DetalheTexto.Contains(termo, StringComparison.OrdinalIgnoreCase)).ToList(); // tambem acha por retirante, documento, destino
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
