using SigaCean.Domain;
using SigaCean.Services;

namespace SigaCean.Presentation.Views;

public partial class RegistrarEntradaPage : ContentPage
{
    private Guid? _produtoInicialId;

    // Abre a tela ja com um produto escolhido (vindo da ficha do produto).
    public void PreSelecionar(Guid produtoId) => _produtoInicialId = produtoId;

    private readonly ProdutoService _produtoService;
    private readonly MovimentacaoService _movimentacaoService;
    private readonly UsuarioService _usuarioService;
    private readonly AvisoService _avisoService;

    private static readonly (TipoOrigemEntrada Valor, string Texto)[] TiposOrigem =
    [
        (TipoOrigemEntrada.Compra, "Compra"),
        (TipoOrigemEntrada.Concessao, "Concessão"),
        (TipoOrigemEntrada.Doacao, "Doação")
    ];

    public RegistrarEntradaPage(ProdutoService produtoService, MovimentacaoService movimentacaoService, UsuarioService usuarioService, AvisoService avisoService)
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

        if (_produtoInicialId is { } id)
        {
            var inicial = _produtoService.ObterTodos().FirstOrDefault(p => p.Id == id);
            if (inicial is not null)
            {
                ProdutoSeletorControl.Selecionar(inicial);
                QuantidadeEntry.Focus(); // produto ja escolhido: o proximo passo e a quantidade
            }
            _produtoInicialId = null;
        }

        TipoOrigemPicker.ItemsSource = TiposOrigem.Select(t => t.Texto).ToList();
    }

    // Enter no ultimo campo registra a entrada (a conferencia continua sendo exibida antes de gravar).
    private void OnCompleted(object? sender, EventArgs e) => OnRegistrarClicked(sender, e);

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
            $"Origem: {TiposOrigem[TipoOrigemPicker.SelectedIndex].Texto}" +
            DescreverValidade(dataValidade),
            "Confirmar", "Revisar");
        if (!confirmou) return;

        try
        {
            _movimentacaoService.RegistrarEntrada(
                produto,
                quantidade,
                tipoOrigem,
                string.IsNullOrWhiteSpace(NumeroAfEntry.Text) ? null : NumeroAfEntry.Text.Trim(),
                string.IsNullOrWhiteSpace(DetalheOrigemEntry.Text) ? null : DetalheOrigemEntry.Text.Trim(),
                dataValidade,
                _usuarioService.UsuarioLogado,
                string.IsNullOrWhiteSpace(DocumentoEntry.Text) ? null : DocumentoEntry.Text.Trim());
        }
        catch (InvalidOperationException ex)
        {
            MostrarErro(ex.Message);
            return;
        }

        _avisoService.Publicar($"Entrada registrada: {produto.Nome} (+{quantidade}). Estoque agora: {produto.QuantidadeAtual}.");
        await Navigation.PopAsync();
    }

    // Mostra a validade na conferencia; uma data ja passada e avisada, pois o produto entraria como vencido.
    private static string DescreverValidade(DateOnly? dataValidade)
    {
        if (dataValidade is null) return string.Empty;

        var aviso = dataValidade < DateOnly.FromDateTime(DateTime.Today) ? " (JÁ VENCIDA)" : string.Empty;
        return $"\nValidade: {dataValidade:dd/MM/yyyy}{aviso}";
    }

    private void MostrarErro(string mensagem)
    {
        MensagemErroLabel.Text = mensagem;
        MensagemErroLabel.IsVisible = true;
    }
}
