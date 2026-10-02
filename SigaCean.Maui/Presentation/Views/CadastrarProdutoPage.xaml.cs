using SigaCean.Domain;
using SigaCean.Services;

namespace SigaCean.Presentation.Views;

public partial class CadastrarProdutoPage : ContentPage
{
    private readonly ProdutoService _produtoService;
    private Guid? _produtoId;

    private static readonly (CategoriaProduto Valor, string Texto)[] Categorias =
    [
        (CategoriaProduto.Alimentar, "Alimentar"),
        (CategoriaProduto.Insumo, "Insumo"),
        (CategoriaProduto.EquipamentoHospitalar, "Equipamento hospitalar"),
        (CategoriaProduto.Medicamento, "Medicamento")
    ];

    public CadastrarProdutoPage(ProdutoService produtoService)
    {
        InitializeComponent();
        _produtoService = produtoService;
        CategoriaPicker.ItemsSource = Categorias.Select(c => c.Texto).ToList();
    }

    public void CarregarParaEdicao(Guid produtoId)
    {
        _produtoId = produtoId;
        var produto = _produtoService.ObterPorId(produtoId);

        Title = "Editar Produto";
        TituloLabel.Text = "Editar produto";
        NomeEntry.Text = produto.Nome;
        ApresentacaoEntry.Text = produto.ApresentacaoEspecificacao;
        EstoqueMinimoEntry.Text = produto.EstoqueMinimo?.ToString();
        CategoriaPicker.SelectedIndex = Array.FindIndex(Categorias, c => c.Valor == produto.Categoria);
    }

    private async void OnSalvarClicked(object? sender, EventArgs e)
    {
        MensagemErroLabel.IsVisible = false;

        if (string.IsNullOrWhiteSpace(NomeEntry.Text))
        {
            MostrarErro("Informe o nome do produto.");
            return;
        }

        if (CategoriaPicker.SelectedIndex < 0)
        {
            MostrarErro("Selecione a categoria.");
            return;
        }

        int? estoqueMinimo = null;
        if (!string.IsNullOrWhiteSpace(EstoqueMinimoEntry.Text))
        {
            if (!int.TryParse(EstoqueMinimoEntry.Text, out var valor) || valor < 0)
            {
                MostrarErro("Informe um estoque mínimo válido.");
                return;
            }
            estoqueMinimo = valor;
        }

        var categoria = Categorias[CategoriaPicker.SelectedIndex].Valor;
        var apresentacao = string.IsNullOrWhiteSpace(ApresentacaoEntry.Text) ? null : ApresentacaoEntry.Text;

        var existente = _produtoService.ObterPorNomeSemelhante(NomeEntry.Text, _produtoId);
        if (existente is not null)
        {
            var continuar = await DisplayAlertAsync(
                "Produto parecido já existe",
                $"Já existe um produto cadastrado como \"{existente.Nome}\". Deseja cadastrar mesmo assim?",
                "Cadastrar mesmo assim",
                "Cancelar");

            if (!continuar) return;
        }

        if (_produtoId is null)
        {
            _produtoService.Cadastrar(NomeEntry.Text, categoria, apresentacao, estoqueMinimo);
        }
        else
        {
            _produtoService.Alterar(_produtoId.Value, NomeEntry.Text, categoria, apresentacao, estoqueMinimo);
        }

        await Navigation.PopAsync();
    }

    private void MostrarErro(string mensagem)
    {
        MensagemErroLabel.Text = mensagem;
        MensagemErroLabel.IsVisible = true;
    }
}
