using Microsoft.Extensions.DependencyInjection;
using SigaCean.Domain;
using SigaCean.Services;
using SigaCean.Presentation.ViewModels;

namespace SigaCean.Presentation.Views;

public partial class EstoquePage : ContentPage
{
	private static readonly (CategoriaProduto? Valor, string Texto)[] Categorias = Rotulos.CategoriasParaFiltro;

	private static readonly (SituacaoEstoque Valor, string Texto)[] Situacoes =
	[
		(SituacaoEstoque.Todos, "Todos"),
		(SituacaoEstoque.SemEstoque, "Sem estoque"),
		(SituacaoEstoque.Critico, "Estoque crítico"),
		(SituacaoEstoque.AbaixoDoMinimo, "Abaixo do mínimo"),
		(SituacaoEstoque.ValidadeProxima, "Validade próxima"),
		(SituacaoEstoque.Vencido, "Vencido")
	];

	private readonly EstoqueService _estoqueService;
	private readonly UsuarioService _usuarioService;
	private readonly IServiceProvider _serviceProvider;
	private readonly AvisoService _avisoService;
	private bool _carregandoFiltros;
	private SituacaoEstoque _situacaoAtual = SituacaoEstoque.Todos;

	public EstoquePage(EstoqueService estoqueService, UsuarioService usuarioService, IServiceProvider serviceProvider, AvisoService avisoService)
	{
		InitializeComponent();
		_estoqueService = estoqueService;
		_usuarioService = usuarioService;
		_serviceProvider = serviceProvider;
		_avisoService = avisoService;

		_carregandoFiltros = true;
		CategoriaFiltroPicker.ItemsSource = Categorias.Select(c => c.Texto).ToList();
		CategoriaFiltroPicker.SelectedIndex = 0;
		_carregandoFiltros = false;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		DesenharChipsDeSituacao();
		CarregarProdutos();
		PreencherUsuarioLogado();
		AvisoBannerControl.Mostrar(_avisoService.Consumir());
	}

	private void PreencherUsuarioLogado()
	{
		var usuario = _usuarioService.UsuarioLogado;
		AcoesAdministrativoLayout.IsVisible = usuario?.Perfil == Perfil.Administrativo;
		CabecalhoControl.Atualizar(usuario);
	}

	// Usado pelo Painel: abre o Estoque ja filtrado por uma situacao.
	public void AplicarSituacao(SituacaoEstoque situacao)
	{
		_situacaoAtual = situacao;
		PesquisaEntry.Text = string.Empty;
		_carregandoFiltros = true;
		CategoriaFiltroPicker.SelectedIndex = 0;
		_carregandoFiltros = false;
		DesenharChipsDeSituacao();
		CarregarProdutos();
	}

	// Um botao por situacao, com quantos produtos ha em cada uma; o escolhido fica destacado.
	private void DesenharChipsDeSituacao()
	{
		SituacaoChipsLayout.Children.Clear();
		foreach (var (valor, texto) in Situacoes)
		{
			var quantidade = _estoqueService.Contar(valor);
			var escolhido = valor == _situacaoAtual;
			var chip = new Button
			{
				Text = $"{texto} ({quantidade})",
				Style = escolhido ? Estilo("ChipEscolhido") : Estilo("Chip"),
				Margin = new Thickness(3)
			};
			SemanticProperties.SetDescription(chip, $"Filtrar por situação: {texto}, {quantidade} produtos");
			chip.Clicked += (_, _) =>
			{
				_situacaoAtual = valor;
				DesenharChipsDeSituacao();
				CarregarProdutos();
			};
			SituacaoChipsLayout.Children.Add(chip);
		}
	}

	private static Style Estilo(string chave) => (Style)Application.Current!.Resources[chave];

	private void CarregarProdutos()
	{
		var categoria = Categorias[Math.Max(CategoriaFiltroPicker.SelectedIndex, 0)].Valor;

		var produtos = _estoqueService.Filtrar(PesquisaEntry.Text, categoria, _situacaoAtual);

		ProdutosCollectionView.ItemsSource = produtos
			.Select(p => new ProdutoEstoqueItem { Produto = p })
			.ToList();

		ResultadoLabel.Text = produtos.Count == 1 ? "1 produto encontrado." : $"{produtos.Count} produtos encontrados.";
	}

	private void OnPesquisaTextChanged(object? sender, TextChangedEventArgs e) => CarregarProdutos();

	private void OnFiltroChanged(object? sender, EventArgs e)
	{
		if (_carregandoFiltros) return;
		CarregarProdutos();
	}

	private async void OnEntradaClicked(object? sender, EventArgs e)
	{
		var pagina = _serviceProvider.GetRequiredService<RegistrarEntradaPage>();
		await Navigation.PushAsync(pagina);
	}

	private async void OnSaidaClicked(object? sender, EventArgs e)
	{
		var pagina = _serviceProvider.GetRequiredService<RegistrarSaidaPage>();
		await Navigation.PushAsync(pagina);
	}

	private async void OnNovoProdutoClicked(object? sender, EventArgs e)
	{
		var pagina = _serviceProvider.GetRequiredService<CadastrarProdutoPage>();
		await Navigation.PushAsync(pagina);
	}

	// Os dois perfis veem a ficha do produto (consulta); as acoes administrativas ficam escondidas para o Veterinario.
	private async void OnProdutoTapped(object? sender, TappedEventArgs e)
	{
		if (sender is not Border border || border.BindingContext is not ProdutoEstoqueItem item) return;

		var pagina = _serviceProvider.GetRequiredService<ProdutoDetalhePage>();
		pagina.Carregar(item.Produto.Id);
		await Navigation.PushAsync(pagina);
	}

	private async void OnAjusteClicked(object? sender, EventArgs e)
	{
		var pagina = _serviceProvider.GetRequiredService<AjusteBalancoPage>();
		await Navigation.PushAsync(pagina);
	}
}
