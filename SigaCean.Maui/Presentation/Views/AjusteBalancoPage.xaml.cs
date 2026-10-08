using SigaCean.Domain;
using SigaCean.Services;

namespace SigaCean.Presentation.Views;

public partial class AjusteBalancoPage : ContentPage
{
    private readonly ProdutoService _produtoService;
    private readonly MovimentacaoService _movimentacaoService;
    private readonly UsuarioService _usuarioService;
    private readonly AvisoService _avisoService;

    public AjusteBalancoPage(ProdutoService produtoService, MovimentacaoService movimentacaoService, UsuarioService usuarioService, AvisoService avisoService)
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
        QuantidadeAtualLabel.Text = string.Empty;
        DiferencaLabel.Text = string.Empty;
    }

    private void OnProdutoSelecionado(object? sender, EventArgs e)
    {
        var produto = ProdutoSeletorControl.ProdutoSelecionado;
        QuantidadeAtualLabel.Text = produto is null ? string.Empty : $"Quantidade atual no sistema: {produto.QuantidadeAtual}";
        DiferencaLabel.Text = string.Empty;
    }

    private void OnQuantidadeApuradaChanged(object? sender, TextChangedEventArgs e)
    {
        var produto = ProdutoSeletorControl.ProdutoSelecionado;
        if (produto is null || !int.TryParse(e.NewTextValue, out var quantidadeApurada))
        {
            DiferencaLabel.Text = string.Empty;
            return;
        }

        var diferenca = quantidadeApurada - produto.QuantidadeAtual;
        DiferencaLabel.Text = diferenca switch
        {
            0 => "Sem diferença em relação ao sistema.",
            > 0 => $"Diferença: +{diferenca} (será registrada como entrada)",
            _ => $"Diferença: {diferenca} (será registrada como saída)"
        };
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

        if (!int.TryParse(QuantidadeApuradaEntry.Text, out var quantidadeApurada) || quantidadeApurada < 0)
        {
            MostrarErro("Informe a quantidade apurada na contagem.");
            return;
        }

        var confirmou = await DisplayAlertAsync(
            "Confirmar ajuste de balanço",
            $"Produto: {produto.Nome}\nEstoque: {produto.QuantidadeAtual} → {quantidadeApurada}",
            "Confirmar", "Revisar");
        if (!confirmou) return;

        try
        {
            _movimentacaoService.RegistrarAjusteBalanco(produto, quantidadeApurada, _usuarioService.UsuarioLogado);
        }
        catch (InvalidOperationException ex)
        {
            MostrarErro(ex.Message);
            return;
        }

        _avisoService.Publicar($"Ajuste de balanço registrado: {produto.Nome}. Estoque agora: {produto.QuantidadeAtual}.");
        await Navigation.PopAsync();
    }

    private void MostrarErro(string mensagem)
    {
        MensagemErroLabel.Text = mensagem;
        MensagemErroLabel.IsVisible = true;
    }
}
