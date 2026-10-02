using SigaCean.Domain;

namespace SigaCean.Presentation.Views;

// Escolha de produto por busca: o estoque do CEAN e extenso, entao basta digitar parte do nome e os resultados
// aparecem logo abaixo para clicar. Sempre escolhe um produto ja cadastrado, nunca texto livre (RD0024).
public partial class ProdutoSeletor : ContentView
{
    private const int MaximoNaLista = 50;

    private IReadOnlyList<Produto> _todos = [];
    private Produto? _selecionado;
    private bool _atualizandoTexto;

    public ProdutoSeletor()
    {
        InitializeComponent();
    }

    public Produto? ProdutoSelecionado => _selecionado;

    public event EventHandler? ProdutoSelecionadoChanged;

    public void Carregar(IEnumerable<Produto> produtos)
    {
        _todos = produtos.OrderBy(p => p.Nome).ToList();
        Definir(null);
        ListaBorder.IsVisible = false;
        ResumoLabel.Text = string.Empty;
    }

    private void OnBuscaFocused(object? sender, FocusEventArgs e)
    {
        // Ao entrar no campo sem produto escolhido, ja mostra a lista para escolher.
        if (_selecionado is null) MostrarResultados(BuscaEntry.Text);
    }

    // Pequeno atraso para o clique em um resultado acontecer antes de a lista sumir.
    private void OnBuscaUnfocused(object? sender, FocusEventArgs e) =>
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), () =>
        {
            if (!BuscaEntry.IsFocused) ListaBorder.IsVisible = false;
        });

    private void OnBuscaChanged(object? sender, TextChangedEventArgs e)
    {
        if (_atualizandoTexto) return;

        // Mexer no texto desfaz a escolha anterior e volta a buscar.
        if (_selecionado is not null)
        {
            _selecionado = null;
            ProdutoSelecionadoChanged?.Invoke(this, EventArgs.Empty);
        }

        MostrarResultados(e.NewTextValue);
    }

    private void MostrarResultados(string? termo)
    {
        var termoLimpo = termo?.Trim();
        var encontrados = string.IsNullOrEmpty(termoLimpo)
            ? _todos
            : _todos.Where(p => p.Nome.Contains(termoLimpo, StringComparison.CurrentCultureIgnoreCase)).ToList();

        ListaLayout.Children.Clear();
        foreach (var produto in encontrados.Take(MaximoNaLista))
            ListaLayout.Children.Add(CriarLinha(produto));

        ListaBorder.IsVisible = encontrados.Count > 0;

        ResumoLabel.Text = encontrados.Count switch
        {
            0 when !string.IsNullOrEmpty(termoLimpo) => "Nenhum produto encontrado com esse nome.",
            > MaximoNaLista => $"{encontrados.Count} produtos encontrados — mostrando os {MaximoNaLista} primeiros; continue digitando para filtrar.",
            1 => "1 produto encontrado.",
            _ when !string.IsNullOrEmpty(termoLimpo) => $"{encontrados.Count} produtos encontrados.",
            _ => string.Empty
        };
    }

    private View CriarLinha(Produto produto)
    {
        var linha = new Border
        {
            Padding = new Thickness(12, 10),
            StrokeThickness = 0,
            BackgroundColor = Colors.Transparent,
            Content = new Label { Text = produto.Nome, FontSize = 13, LineBreakMode = LineBreakMode.TailTruncation }
        };
        var toque = new TapGestureRecognizer();
        toque.Tapped += (_, _) => Definir(produto);
        linha.GestureRecognizers.Add(toque);
        return linha;
    }

    // Escolhe (ou limpa) o produto, escreve o nome no campo e fecha a lista.
    private void Definir(Produto? produto)
    {
        _selecionado = produto;

        _atualizandoTexto = true;
        BuscaEntry.Text = produto?.Nome ?? string.Empty;
        _atualizandoTexto = false;

        if (produto is not null)
        {
            ListaBorder.IsVisible = false;
            ResumoLabel.Text = string.Empty;
        }

        ProdutoSelecionadoChanged?.Invoke(this, EventArgs.Empty);
    }
}
