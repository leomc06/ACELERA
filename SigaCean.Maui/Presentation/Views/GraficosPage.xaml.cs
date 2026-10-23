using SigaCean.Domain;
using SigaCean.Presentation.ViewModels;
using SigaCean.Services;

namespace SigaCean.Presentation.Views;

// Graficos do estoque por categoria (Administrativo). Os numeros vem do RelatorioService.GerarConsumoPorCategoria;
// a tela so escolhe como mostrar (barras, pizza ou rosca) e desenha.
public partial class GraficosPage : ContentPage
{
    private const int MaximoDeProdutosNoDetalhe = 10;
    private const float AlturaDaLinhaDoDetalhe = 34;

    private readonly RelatorioService _relatorioService;
    private readonly UsuarioService _usuarioService;
    private readonly GraficoDrawable _principal = new();
    private readonly GraficoDrawable _detalhe = new() { Tipo = TipoGrafico.BarrasHorizontais };

    private IReadOnlyList<ConsumoPorCategoria> _consumo = [];
    private TipoGrafico _tipo = TipoGrafico.Barras;
    private bool _carregando;
    private bool _periodoInicializado;
    private bool _menosUsados; // o grafico de produtos mostra os mais usados (padrao) ou os menos usados

    public GraficosPage(RelatorioService relatorioService, UsuarioService usuarioService)
    {
        InitializeComponent();
        _relatorioService = relatorioService;
        _usuarioService = usuarioService;

        _carregando = true;
        MetricaPicker.ItemsSource = new[] { "Estoque atual (unidades)", "Uso no período (saídas)", "Produtos cadastrados" };
        MetricaPicker.SelectedIndex = 0;
        CategoriaPicker.ItemsSource = Rotulos.Categorias.Select(Rotulos.Categoria).ToList();
        _carregando = false;

        GraficoView.Drawable = _principal;
        DetalheView.Drawable = _detalhe;
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

    // ---------- Escolhas da tela ----------

    private void OnBarrasClicked(object? sender, EventArgs e) => TrocarTipo(TipoGrafico.Barras);
    private void OnPizzaClicked(object? sender, EventArgs e) => TrocarTipo(TipoGrafico.Pizza);
    private void OnRoscaClicked(object? sender, EventArgs e) => TrocarTipo(TipoGrafico.Rosca);

    private void TrocarTipo(TipoGrafico tipo)
    {
        _tipo = tipo;
        MarcarTipo(BarrasButton, _tipo == TipoGrafico.Barras);
        MarcarTipo(PizzaButton, _tipo == TipoGrafico.Pizza);
        MarcarTipo(RoscaButton, _tipo == TipoGrafico.Rosca);
        AtualizarPrincipal();
    }

    // O tipo escolhido fica preenchido; os outros so com contorno (estilos Chip e ChipEscolhido).
    private static void MarcarTipo(Button botao, bool selecionado) =>
        botao.Style = (Style)Application.Current!.Resources[selecionado ? "ChipEscolhido" : "Chip"];

    private void OnMetricaChanged(object? sender, EventArgs e)
    {
        if (!_carregando) AtualizarPrincipal();
    }

    private void OnPeriodoAlterado(object? sender, EventArgs e) => Atualizar();

    private void OnMaisUsadosClicked(object? sender, EventArgs e) => TrocarOrdemDoDetalhe(menosUsados: false);
    private void OnMenosUsadosClicked(object? sender, EventArgs e) => TrocarOrdemDoDetalhe(menosUsados: true);

    private void TrocarOrdemDoDetalhe(bool menosUsados)
    {
        _menosUsados = menosUsados;
        MarcarTipo(MaisUsadosButton, !menosUsados);
        MarcarTipo(MenosUsadosButton, menosUsados);
        AtualizarDetalhe();
    }

    private void OnCategoriaChanged(object? sender, EventArgs e)
    {
        if (!_carregando) AtualizarDetalhe();
    }

    // ---------- Atualizacao ----------

    private void Atualizar()
    {
        _consumo = _relatorioService.GerarConsumoPorCategoria(SeletorPeriodo.Inicio, SeletorPeriodo.FimInclusivo);
        AvisoPeriodoLabel.Text = $"{SeletorPeriodo.Descricao}. O período vale para o uso (saídas); o estoque atual e o número de produtos são sempre de hoje.";

        AtualizarPrincipal();
        AtualizarCartoes();

        // Se ainda nao ha categoria escolhida (ou ela ficou sem uso), abre o detalhe na mais usada do periodo.
        if (CategoriaPicker.SelectedIndex < 0 || _consumo.All(c => c.TotalSaidas == 0))
        {
            var maisUsada = _consumo.Where(c => c.TotalSaidas > 0).OrderByDescending(c => c.TotalSaidas).FirstOrDefault();
            _carregando = true;
            CategoriaPicker.SelectedIndex = Array.IndexOf(Rotulos.Categorias, maisUsada?.Categoria ?? Rotulos.Categorias[0]);
            _carregando = false;
        }
        AtualizarDetalhe();
    }

    private void AtualizarPrincipal()
    {
        var metrica = MetricaPicker.SelectedIndex;
        int Valor(ConsumoPorCategoria c) => metrica switch
        {
            1 => c.TotalSaidas,
            2 => c.QuantidadeDeProdutos,
            _ => c.EstoqueAtual
        };

        TituloGraficoLabel.Text = metrica switch
        {
            1 => "Uso por categoria (unidades que saíram no período)",
            2 => "Produtos cadastrados por categoria",
            _ => "Estoque atual por categoria (unidades)"
        };

        // Categorias na ordem de sempre das telas (Medicamento primeiro).
        var fatias = Rotulos.Categorias
            .Select(cat => _consumo.First(c => c.Categoria == cat))
            .Select(c => new FatiaGrafico(Rotulos.Categoria(c.Categoria), Valor(c), PaletaDeCategorias.Cor(c.Categoria)))
            .ToList();
        var total = fatias.Sum(f => f.Valor);

        _principal.Tipo = _tipo;
        _principal.Fatias = fatias;
        _principal.TextoCentral = metrica switch { 1 => "unidades usadas", 2 => "produtos", _ => "unidades" };
        _principal.MensagemVazia = metrica == 1 ? "Nenhuma saída registrada neste período." : "Nada cadastrado ainda.";
        GraficoView.Invalidate();

        BindableLayout.SetItemsSource(LegendaLista, fatias.Select(f => new LegendaItem
        {
            Rotulo = f.Rotulo,
            ValorTexto = TextosDeConsumo.Numero(f.Valor),
            PercentualTexto = TextosDeConsumo.Percentual(f.Valor, total),
            Cor = f.Cor
        }).ToList());
    }

    private void AtualizarCartoes()
    {
        // As categorias com produtos primeiro; dentro delas, as mais usadas no topo.
        BindableLayout.SetItemsSource(CartoesLista, _consumo
            .OrderByDescending(c => c.QuantidadeDeProdutos > 0)
            .ThenByDescending(c => c.TotalSaidas)
            .Select(c => new CategoriaCartao(c, _consumo.Sum(x => x.TotalSaidas)))
            .ToList());
    }

    private void AtualizarDetalhe()
    {
        var indice = CategoriaPicker.SelectedIndex;
        var selecionada = indice >= 0 && indice < Rotulos.Categorias.Length
            ? _consumo.FirstOrDefault(c => c.Categoria == Rotulos.Categorias[indice])
            : null;

        // ProdutosUsados vem do mais para o menos usado; "menos usados" inverte: do menor para o maior (empate: por nome).
        var usados = selecionada?.ProdutosUsados ?? [];
        var mostrados = (_menosUsados ? usados.OrderBy(c => c.TotalSaidas).ThenBy(c => c.Produto.Nome).ToList() : usados.ToList())
            .Take(MaximoDeProdutosNoDetalhe).ToList();
        TituloDetalheLabel.Text = _menosUsados
            ? "Produtos de uma categoria, do menos ao mais usado"
            : "Produtos de uma categoria, do mais ao menos usado";
        var cor = selecionada is null ? PaletaDeCategorias.Cor(CategoriaProduto.Medicamento) : PaletaDeCategorias.Cor(selecionada.Categoria);

        _detalhe.Fatias = mostrados.Select(c => new FatiaGrafico(c.Produto.Nome, c.TotalSaidas, cor)).ToList();
        _detalhe.MensagemVazia = selecionada is null
            ? "Escolha uma categoria."
            : selecionada.QuantidadeDeProdutos == 0
                ? "Nenhum produto cadastrado nesta categoria."
                : "Nenhuma saída desta categoria neste período.";

        DetalheView.HeightRequest = Math.Max(110, mostrados.Count * AlturaDaLinhaDoDetalhe + 12);
        DetalheView.Invalidate();

        DetalheNotaLabel.Text = usados.Count > MaximoDeProdutosNoDetalhe
            ? $"Mostrando os {MaximoDeProdutosNoDetalhe} {(_menosUsados ? "menos" : "mais")} usados, entre os {usados.Count} produtos que tiveram saída no período. Produtos sem nenhuma saída não aparecem."
            : string.Empty;
    }
}
