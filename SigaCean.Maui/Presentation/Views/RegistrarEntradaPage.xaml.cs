using SigaCean.Domain;
using SigaCean.Services;

namespace SigaCean.Presentation.Views;

public partial class RegistrarEntradaPage : ContentPage
{
    private readonly ProdutoService _produtoService;
    private readonly MovimentacaoService _movimentacaoService;
    private readonly UsuarioService _usuarioService;

    private static readonly (TipoOrigemEntrada Valor, string Texto)[] TiposOrigem =
    [
        (TipoOrigemEntrada.Compra, "Compra"),
        (TipoOrigemEntrada.Concessao, "Concessão"),
        (TipoOrigemEntrada.Doacao, "Doação")
    ];

    public RegistrarEntradaPage(ProdutoService produtoService, MovimentacaoService movimentacaoService, UsuarioService usuarioService)
    {
        InitializeComponent();
        _produtoService = produtoService;
        _movimentacaoService = movimentacaoService;
        _usuarioService = usuarioService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        ProdutoSeletorControl.Carregar(_produtoService.ObterTodos());

        TipoOrigemPicker.ItemsSource = TiposOrigem.Select(t => t.Texto).ToList();
    }

    private void OnPossuiValidadeToggled(object? sender, ToggledEventArgs e)
    {
        ValidadeDatePicker.IsVisible = e.Value;
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

        if (TipoOrigemPicker.SelectedIndex < 0)
        {
            MostrarErro("Selecione o tipo de origem.");
            return;
        }

        var tipoOrigem = TiposOrigem[TipoOrigemPicker.SelectedIndex].Valor;
        var dataValidade = PossuiValidadeSwitch.IsToggled
            ? DateOnly.FromDateTime(ValidadeDatePicker.Date ?? DateTime.Today)
            : (DateOnly?)null;

        // Confere antes de gravar: erro de quantidade e um dos problemas mais comuns da planilha.
        var confirmou = await DisplayAlertAsync(
            "Confirmar entrada",
            $"Produto: {produto.Nome}\n" +
            $"Quantidade: {quantidade}\n" +
            $"Estoque: {produto.QuantidadeAtual} → {produto.QuantidadeAtual + quantidade}\n" +
            $"Origem: {TiposOrigem[TipoOrigemPicker.SelectedIndex].Texto}",
            "Confirmar", "Revisar");
        if (!confirmou) return;

        _movimentacaoService.RegistrarEntrada(
            produto,
            quantidade,
            tipoOrigem,
            string.IsNullOrWhiteSpace(NumeroAfEntry.Text) ? null : NumeroAfEntry.Text.Trim(),
            string.IsNullOrWhiteSpace(DetalheOrigemEntry.Text) ? null : DetalheOrigemEntry.Text.Trim(),
            string.IsNullOrWhiteSpace(LoteEntry.Text) ? null : LoteEntry.Text.Trim(),
            dataValidade,
            _usuarioService.UsuarioLogado,
            string.IsNullOrWhiteSpace(DocumentoEntry.Text) ? null : DocumentoEntry.Text.Trim());

        await Navigation.PopAsync();
    }

    private void MostrarErro(string mensagem)
    {
        MensagemErroLabel.Text = mensagem;
        MensagemErroLabel.IsVisible = true;
    }
}
