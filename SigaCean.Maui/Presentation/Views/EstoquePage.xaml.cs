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
		(SituacaoEstoque.Todos, "Todas as situações"),
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
		AtualizarOpcoesDeSituacao();
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

	// Mostra quantos produtos ha em cada situacao de alerta; a selecao atual e mantida.
	private void AtualizarOpcoesDeSituacao()
	{
		var selecionada = Math.Max(SituacaoFiltroPicker.SelectedIndex, 0);

		_carregandoFiltros = true;
		SituacaoFiltroPicker.ItemsSource = Situacoes
			.Select(s => s.Valor == SituacaoEstoque.Todos ? s.Texto : $"{s.Texto} ({_estoqueService.Contar(s.Valor)})")
			.ToList();
		SituacaoFiltroPicker.SelectedIndex = selecionada;
		_carregandoFiltros = false;
	}

	private void CarregarProdutos()
	{
		var categoria = Categorias[Math.Max(CategoriaFiltroPicker.SelectedIndex, 0)].Valor;
		var situacao = Situacoes[Math.Max(SituacaoFiltroPicker.SelectedIndex, 0)].Valor;

		var produtos = _estoqueService.Filtrar(PesquisaEntry.Text, categoria, situacao);

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

	private async void OnProdutoTapped(object? sender, TappedEventArgs e)
	{
		if (sender is not Border border || border.BindingContext is not ProdutoEstoqueItem item) return;
		if (_usuarioService.UsuarioLogado?.Perfil != Perfil.Administrativo) return;

		var pagina = _serviceProvider.GetRequiredService<CadastrarProdutoPage>();
		pagina.CarregarParaEdicao(item.Produto.Id);
		await Navigation.PushAsync(pagina);
	}

	private async void OnAjusteClicked(object? sender, EventArgs e)
	{
		var pagina = _serviceProvider.GetRequiredService<AjusteBalancoPage>();
		await Navigation.PushAsync(pagina);
	}
}
