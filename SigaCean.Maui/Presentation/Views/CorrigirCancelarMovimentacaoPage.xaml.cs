using SigaCean.Domain;
using SigaCean.Services;
using SigaCean.Presentation.ViewModels;

namespace SigaCean.Presentation.Views;

public partial class CorrigirCancelarMovimentacaoPage : ContentPage
{
    private readonly MovimentacaoService _movimentacaoService;
    private readonly AvisoService _avisoService;
    private Movimentacao? _movimentacao;

    public CorrigirCancelarMovimentacaoPage(MovimentacaoService movimentacaoService, AvisoService avisoService)
    {
        InitializeComponent();
        _movimentacaoService = movimentacaoService;
        _avisoService = avisoService;
    }

    public void CarregarMovimentacao(Movimentacao movimentacao)
    {
        _movimentacao = movimentacao;
        var item = new MovimentacaoHistoricoItem { Movimentacao = movimentacao };

        TipoProdutoLabel.Text = $"{item.TipoTexto} · {item.ProdutoNome} ({item.QuantidadeTexto})";
        DetalheLabel.Text = item.DetalheTexto;
        DataLabel.Text = item.DataFormatada;
    }

    private void OnCorrigirClicked(object? sender, EventArgs e) => Executar(_movimentacaoService.Corrigir, "corrigida");

    private void OnCancelarClicked(object? sender, EventArgs e) => Executar(_movimentacaoService.Cancelar, "cancelada");

    private async void Executar(Action<Guid, string> acao, string resultado)
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

        var lembrete = resultado == "corrigida" ? " Agora registre a movimentação certa." : string.Empty;
        _avisoService.Publicar($"Movimentação {resultado}: {TipoProdutoLabel.Text}. O estoque foi atualizado.{lembrete}");
        await Navigation.PopAsync();
    }
}
