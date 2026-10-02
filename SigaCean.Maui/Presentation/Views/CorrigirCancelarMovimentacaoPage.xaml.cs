using SigaCean.Domain;
using SigaCean.Services;
using SigaCean.Presentation.ViewModels;

namespace SigaCean.Presentation.Views;

public partial class CorrigirCancelarMovimentacaoPage : ContentPage
{
    private readonly MovimentacaoService _movimentacaoService;
    private Movimentacao? _movimentacao;

    public CorrigirCancelarMovimentacaoPage(MovimentacaoService movimentacaoService)
    {
        InitializeComponent();
        _movimentacaoService = movimentacaoService;
    }

    public void CarregarMovimentacao(Movimentacao movimentacao)
    {
        _movimentacao = movimentacao;
        var item = new MovimentacaoHistoricoItem { Movimentacao = movimentacao };

        TipoProdutoLabel.Text = $"{item.TipoTexto} · {item.ProdutoNome} ({item.QuantidadeTexto})";
        DetalheLabel.Text = item.DetalheTexto;
        DataLabel.Text = item.DataFormatada;
    }

    private void OnCorrigirClicked(object? sender, EventArgs e) => Executar(_movimentacaoService.Corrigir);

    private void OnCancelarClicked(object? sender, EventArgs e) => Executar(_movimentacaoService.Cancelar);

    private async void Executar(Action<Guid, string> acao)
    {
        if (_movimentacao is null) return;

        var motivo = MotivoEditor.Text?.Trim();
        if (string.IsNullOrWhiteSpace(motivo))
        {
            ErroLabel.Text = "Informe o motivo antes de continuar.";
            ErroLabel.IsVisible = true;
            return;
        }

        try
        {
            acao(_movimentacao.Id, motivo);
        }
        catch (InvalidOperationException ex)
        {
            ErroLabel.Text = ex.Message;
            ErroLabel.IsVisible = true;
            return;
        }

        await Navigation.PopAsync();
    }
}
