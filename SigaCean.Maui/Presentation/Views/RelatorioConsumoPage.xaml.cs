using SigaCean.Domain;
using SigaCean.Services;
using SigaCean.Presentation.Reports;
using SigaCean.Presentation.ViewModels;

namespace SigaCean.Presentation.Views;

public partial class RelatorioConsumoPage : ContentPage
{
    private static readonly (CategoriaProduto? Valor, string Texto)[] Categorias = Rotulos.CategoriasParaFiltro;

    private static readonly (OrdemRelatorio Valor, string Texto)[] Ordens =
    [
        (OrdemRelatorio.PorNome, "Nome do produto"),
        (OrdemRelatorio.MaiorConsumo, "Maior consumo (saídas)")
    ];

    private readonly RelatorioService _relatorioService;
    private readonly UsuarioService _usuarioService;

    private TipoPeriodo _tipo = TipoPeriodo.Mensal;
    private DateTime _inicio;
    private DateTime _fim;
    private bool _carregando;
    private IReadOnlyList<ItemRelatorioConsumo> _itensDoPeriodo = [];

    public RelatorioConsumoPage(RelatorioService relatorioService, UsuarioService usuarioService)
    {
        InitializeComponent();
        _relatorioService = relatorioService;
        _usuarioService = usuarioService;

        _carregando = true;
        CategoriaPicker.ItemsSource = Categorias.Select(c => c.Texto).ToList();
        CategoriaPicker.SelectedIndex = 0;
        OrdemPicker.ItemsSource = Ordens.Select(o => o.Texto).ToList();
        OrdemPicker.SelectedIndex = 0;
        _carregando = false;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        PesquisaEntry.Text = string.Empty;
        PreencherUsuarioLogado();
        EscolherTipo(TipoPeriodo.Mensal);
    }

    private void PreencherUsuarioLogado()
    {
        var usuario = _usuarioService.UsuarioLogado;
        CabecalhoControl.Atualizar(usuario);
    }

    // ---- Periodo ----

    private void OnTipoPeriodoClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is string nome && Enum.TryParse<TipoPeriodo>(nome, out var tipo))
            EscolherTipo(tipo);
    }

    private void EscolherTipo(TipoPeriodo tipo)
    {
        _tipo = tipo;

        if (tipo == TipoPeriodo.Personalizado)
        {
            // Parte do ultimo periodo mostrado, para o usuario so ajustar as datas.
            _carregando = true;
            DataInicioPicker.Date = _inicio == default ? DateTime.Today.AddDays(-30) : _inicio;
            DataFimPicker.Date = _fim == default ? DateTime.Today : _fim;
            _carregando = false;
        }
        else
        {
            (_inicio, _fim) = PeriodoRelatorio.Contendo(tipo, DateTime.Today);
        }

        AtualizarBotoesDeTipo();
        GerarRelatorio();
    }

    private void OnPeriodoAnteriorClicked(object? sender, EventArgs e) => Deslocar(-1);

    private void OnPeriodoProximoClicked(object? sender, EventArgs e) => Deslocar(1);

    private void Deslocar(int passos)
    {
        (_inicio, _fim) = PeriodoRelatorio.Deslocar(_tipo, _inicio, passos);
        GerarRelatorio();
    }

    private void OnGerarClicked(object? sender, EventArgs e) => GerarRelatorio();

    private void AtualizarBotoesDeTipo()
    {
        Marcar(MensalButton, _tipo == TipoPeriodo.Mensal);
        Marcar(TrimestralButton, _tipo == TipoPeriodo.Trimestral);
        Marcar(AnualButton, _tipo == TipoPeriodo.Anual);
        Marcar(PersonalizadoButton, _tipo == TipoPeriodo.Personalizado);

        NavegacaoPeriodoLayout.IsVisible = _tipo != TipoPeriodo.Personalizado;
        PersonalizadoLayout.IsVisible = _tipo == TipoPeriodo.Personalizado;
    }

    // O tipo escolhido fica em destaque (cor cheia); os outros ficam com fundo claro.
    private static void Marcar(Button botao, bool selecionado)
    {
        var principal = (Color)Application.Current!.Resources["Primary"];
        botao.BackgroundColor = selecionado ? principal : Color.FromArgb("#E6F2F1");
        botao.TextColor = selecionado ? Colors.White : principal;
        botao.BorderColor = principal;
    }

    private void GerarRelatorio()
    {
        if (_tipo == TipoPeriodo.Personalizado)
        {
            var inicio = DataInicioPicker.Date ?? DateTime.Today;
            var fim = DataFimPicker.Date ?? DateTime.Today;
            if (inicio > fim)
            {
                MostrarMensagem("A data inicial não pode ser depois da data final.");
                return;
            }
            (_inicio, _fim) = (inicio, fim);
        }

        MostrarMensagem(null);
        PeriodoLabel.Text = PeriodoRelatorio.Descrever(_tipo, _inicio, _fim);

        var categoria = Categorias[Math.Max(CategoriaPicker.SelectedIndex, 0)].Valor;
        var ordem = Ordens[Math.Max(OrdemPicker.SelectedIndex, 0)].Valor;
        _itensDoPeriodo = _relatorioService.GerarRelatorioConsumo(_inicio, _fim.AddDays(1).AddTicks(-1), categoria, ordem); // inclui o dia final por inteiro
        AtualizarLista();
    }

    // ---- Filtros e lista ----

    private void OnFiltroChanged(object? sender, EventArgs e)
    {
        if (_carregando) return;
        GerarRelatorio();
    }

    private void OnPesquisaTextChanged(object? sender, TextChangedEventArgs e) => AtualizarLista();

    // Itens do periodo que passam pela pesquisa por nome; e o que aparece na tela, no resumo e no PDF.
    private List<ItemRelatorioConsumo> ItensFiltrados()
    {
        var termo = PesquisaEntry.Text?.Trim();
        return string.IsNullOrEmpty(termo)
            ? _itensDoPeriodo.ToList()
            : _itensDoPeriodo.Where(i => BuscaTexto.Contem(i.Produto.Nome, termo)).ToList();
    }

    private void AtualizarLista()
    {
        var itens = ItensFiltrados();

        RelatorioCollectionView.ItemsSource = itens.Select(item => new RelatorioConsumoItem { Item = item }).ToList();
        SemResultadosLabel.IsVisible = itens.Count == 0;
        SemResultadosLabel.Text = TextoSemResultados(itens.Count == 0 && _itensDoPeriodo.Count == 0);

        var ajustes = itens.Sum(i => i.TotalAjustes);
        ResumoProdutosLabel.Text = itens.Count.ToString();
        ResumoEntradasLabel.Text = itens.Sum(i => i.TotalEntradas).ToString();
        ResumoSaidasLabel.Text = itens.Sum(i => i.TotalSaidas).ToString();
        ResumoAjustesLabel.Text = ajustes > 0 ? $"+{ajustes}" : ajustes.ToString();

        GerarPdfButton.IsEnabled = itens.Count > 0;
    }

    // Periodo sem nenhuma movimentacao: diz onde esta o ultimo registro, para ninguem achar que o relatorio quebrou.
    private string TextoSemResultados(bool periodoVazio)
    {
        const string padrao = "Nenhuma movimentação encontrada no período.";
        if (!periodoVazio || _relatorioService.DataDaUltimaMovimentacao() is not { } ultima) return padrao;

        return $"{padrao} A última movimentação registrada é de {ultima:MMMM 'de' yyyy}; use \"Anterior\" para chegar até lá.";
    }

    // ---- PDF ----

    private async void OnGerarPdfClicked(object? sender, EventArgs e)
    {
        var itens = ItensFiltrados();
        if (itens.Count == 0) return;

        try
        {
            var caminho = await EscolherArquivoParaSalvarAsync(RelatorioConsumoPdf.NomeSugerido(_tipo, _inicio, _fim));
            if (caminho is null) return; // usuario cancelou a escolha

            var categoria = Categorias[Math.Max(CategoriaPicker.SelectedIndex, 0)].Texto;
            RelatorioConsumoPdf.Gerar(caminho, itens, _tipo, _inicio, _fim, categoria, _usuarioService.UsuarioLogado?.Nome);
            MostrarMensagem($"PDF salvo em: {caminho}");

            await Launcher.Default.OpenAsync(new OpenFileRequest("Relatório de Consumo", new ReadOnlyFile(caminho)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MostrarMensagem("Não foi possível salvar o PDF. Verifique se o arquivo não está aberto em outro programa e tente novamente.");
        }
    }

    // Janela "Salvar como" do Windows; devolve null se o usuario cancelar.
    private static async Task<string?> EscolherArquivoParaSalvarAsync(string nomeSugerido)
    {
        var seletor = new Windows.Storage.Pickers.FileSavePicker
        {
            SuggestedFileName = Path.GetFileNameWithoutExtension(nomeSugerido),
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
        };
        seletor.FileTypeChoices.Add("Documento PDF", [".pdf"]);

        var janela = Application.Current?.Windows[0].Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (janela is null) return null;
        WinRT.Interop.InitializeWithWindow.Initialize(seletor, WinRT.Interop.WindowNative.GetWindowHandle(janela));

        var arquivo = await seletor.PickSaveFileAsync();
        return arquivo?.Path;
    }

    private void MostrarMensagem(string? texto)
    {
        MensagemLabel.Text = texto ?? string.Empty;
        MensagemLabel.IsVisible = !string.IsNullOrEmpty(texto);
    }
}
