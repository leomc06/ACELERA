using SigaCean.Domain;
using SigaCean.Services;

namespace SigaCean.Presentation.Views;

public partial class RegistrarSaidaPage : ContentPage
{
    private readonly ProdutoService _produtoService;
    private readonly MovimentacaoService _movimentacaoService;
    private readonly UsuarioService _usuarioService;
    private readonly AvisoService _avisoService;

    public RegistrarSaidaPage(ProdutoService produtoService, MovimentacaoService movimentacaoService, UsuarioService usuarioService, AvisoService avisoService)
    {
        InitializeComponent();
        _produtoService = produtoService;
        _movimentacaoService = movimentacaoService;
        _usuarioService = usuarioService;
        _avisoService = avisoService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        ProdutoSeletorControl.Carregar(_produtoService.ObterTodos());
        DisponivelLabel.Text = string.Empty;
    }

    private void OnProdutoSelecionado(object? sender, EventArgs e)
    {
        var produto = ProdutoSeletorControl.ProdutoSelecionado;
        DisponivelLabel.Text = produto is null ? string.Empty : $"Disponível em estoque: {produto.QuantidadeAtual}";
    }

    private async void OnRegistrarClicked(object? sender, EventArgs e)
    {
        MensagemErroLabel.IsVisible = false;

        var produto = ProdutoSeletorControl.ProdutoSelecionado;
        if (produto is null)
        {
            MostrarErro("Selecione um produto da lista.");
            return;
        }

        if (!int.TryParse(QuantidadeEntry.Text, out var quantidade) || quantidade <= 0)
        {
            MostrarErro("Informe uma quantidade válida.");
            return;
        }

        if (string.IsNullOrWhiteSpace(DestinoEntry.Text))
        {
            MostrarErro("Informe o destino.");
            return;
        }

        if (string.IsNullOrWhiteSpace(RetiradoPorEntry.Text))
        {
            MostrarErro("Informe quem está retirando o produto.");
            return;
        }

        if (quantidade > produto.QuantidadeAtual)
        {
            MostrarErro($"Estoque insuficiente: há apenas {produto.QuantidadeAtual} unidade(s) de \"{produto.Nome}\".");
            return;
        }

        // Confere antes de gravar: erro de quantidade e um dos problemas mais comuns da planilha.
        var confirmou = await DisplayAlertAsync(
            "Confirmar saída",
            $"Produto: {produto.Nome}\n" +
            $"Quantidade: {quantidade}\n" +
            $"Estoque: {produto.QuantidadeAtual} → {produto.QuantidadeAtual - quantidade}\n" +
            $"Destino: {DestinoEntry.Text.Trim()}\n" +
            $"Retirado por: {RetiradoPorEntry.Text.Trim()}",
            "Confirmar", "Revisar");
        if (!confirmou) return;

        try
        {
            _movimentacaoService.RegistrarSaida(produto, quantidade, DestinoEntry.Text.Trim(), _usuarioService.UsuarioLogado, RetiradoPorEntry.Text.Trim());
        }
        catch (InvalidOperationException ex)
        {
            MostrarErro(ex.Message);
            return;
        }

        _avisoService.Publicar($"Saída registrada: {produto.Nome} (−{quantidade}). Estoque agora: {produto.QuantidadeAtual}.");
        await Navigation.PopAsync();
    }

    private void MostrarErro(string mensagem)
    {
        MensagemErroLabel.Text = mensagem;
        MensagemErroLabel.IsVisible = true;
    }
}
