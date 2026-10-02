using SigaCean.Domain;

namespace SigaCean.Presentation.ViewModels;

// Prepara os dados de um Produto para exibicao na tela de Estoque (RF0005/RF0021):
// quantidade, percentual e cor de saude, alem dos alertas de minimo e validade.
public class ProdutoEstoqueItem
{
    public required Produto Produto { get; init; }

    public string Nome => Produto.Nome;
    public string Categoria => Produto.Categoria switch
    {
        CategoriaProduto.Alimentar => "Alimentar",
        CategoriaProduto.Insumo => "Insumo",
        CategoriaProduto.EquipamentoHospitalar => "Equipamento hospitalar",
        CategoriaProduto.Medicamento => "Medicamento",
        _ => Produto.Categoria.ToString()
    };

    public int QuantidadeAtual => Produto.QuantidadeAtual;
    public string EstoqueMinimoTexto => Produto.EstoqueMinimo?.ToString() ?? "não definido";
    public string PercentualTexto => Produto.PercentualSaude is { } p ? $"{p:0}%" : "—";

    public Color CorSaude => Produto.CorSaude == CorSaudeEstoque.Verde
        ? Color.FromArgb("#169B62")
        : Color.FromArgb("#D95757");

    public bool MostrarAlertaCritico => Produto.EstaCritico;
    public bool MostrarAlertaVencido => Produto.TemLoteVencido(DateOnly.FromDateTime(DateTime.Today));
    public bool MostrarAlertaValidadeProxima => !MostrarAlertaVencido &&
        Produto.TemLoteProximoDoVencimento(DateOnly.FromDateTime(DateTime.Today), SigaCean.Services.EstoqueService.DiasAlertaValidade);
}
