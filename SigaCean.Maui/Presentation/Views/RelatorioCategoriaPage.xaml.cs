using SigaCean.Presentation.Reports;
using SigaCean.Presentation.ViewModels;
using SigaCean.Services;

namespace SigaCean.Presentation.Views;

// Segundo relatorio (Administrativo): uso por categoria e, dentro de cada uma, o produto mais e o menos usado.
// Os numeros vem do RelatorioService.GerarConsumoPorCategoria (os mesmos da tela de Graficos); o PDF mostra o mesmo conteudo.
public partial class RelatorioCategoriaPage : ContentPage
{
    private readonly RelatorioService _relatorioService;
    private readonly UsuarioService _usuarioService;

    private IReadOnlyList<ConsumoPorCategoria> _consumo = [];
    private bool _periodoInicializado;

    public RelatorioCategoriaPage(RelatorioService relatorioService, UsuarioService usuarioService)
    {
        InitializeComponent();
        _relatorioService = relatorioService;
        _usuarioService = usuarioService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CabecalhoControl.Atualizar(_usuarioService.UsuarioLogado);

        if (!_periodoInicializado)
        {
            _periodoInicializado = true;
            SeletorPeriodo.Escolher(TipoPeriodo.Anual); // o ano todo: e onde esta o historico do CEAN
        }
        else
        {
            Atualizar(); // volta para a tela: pode ter havido saidas novas
        }
    }

    private void OnPeriodoAlterado(object? sender, EventArgs e) => Atualizar();

    private void Atualizar()
    {
        MostrarMensagem(null);
        _consumo = _relatorioService.GerarConsumoPorCategoria(SeletorPeriodo.Inicio, SeletorPeriodo.FimInclusivo);

        var totalUsado = _consumo.Sum(c => c.TotalSaidas);
        var maisUsada = _consumo.Where(c => c.TotalSaidas > 0).OrderByDescending(c => c.TotalSaidas).FirstOrDefault();

        ResumoUsadoLabel.Text = TextosDeConsumo.Numero(totalUsado);
        ResumoCategoriasLabel.Text = _consumo.Count(c => c.TotalSaidas > 0).ToString();
        ResumoMaisUsadaLabel.Text = maisUsada is null ? "—" : Rotulos.Categoria(maisUsada.Categoria);
        SemUsoLabel.IsVisible = totalUsado == 0;
        GerarPdfButton.IsEnabled = totalUsado > 0;

        // As categorias com mais uso no topo; as sem produto ficam por ultimo.
        BindableLayout.SetItemsSource(CategoriasLista, _consumo
            .OrderByDescending(c => c.TotalSaidas)
            .ThenByDescending(c => c.QuantidadeDeProdutos)
            .Select(c => new CategoriaCartao(c, totalUsado))
            .ToList());
    }

    // ---- PDF ----

    private async void OnGerarPdfClicked(object? sender, EventArgs e)
    {
        try
        {
            var caminho = await SalvarComo.PdfAsync(RelatorioCategoriaPdf.NomeSugerido(SeletorPeriodo.Tipo, SeletorPeriodo.Inicio, SeletorPeriodo.Fim));
            if (caminho is null) return; // usuario cancelou a escolha

            RelatorioCategoriaPdf.Gerar(caminho, _consumo, SeletorPeriodo.Tipo, SeletorPeriodo.Inicio, SeletorPeriodo.Fim, _usuarioService.UsuarioLogado?.Nome);
            MostrarMensagem($"PDF salvo em: {caminho}");

            await Launcher.Default.OpenAsync(new OpenFileRequest("Relatório por Categoria", new ReadOnlyFile(caminho)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MostrarMensagem("Não foi possível salvar o PDF. Verifique se o arquivo não está aberto em outro programa e tente novamente.");
        }
    }

    private void MostrarMensagem(string? texto)
    {
        MensagemLabel.Text = texto ?? string.Empty;
        MensagemLabel.IsVisible = !string.IsNullOrEmpty(texto);
    }
}
