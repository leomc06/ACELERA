using SigaCean.Domain;

namespace SigaCean.Presentation.ViewModels;

// Prepara os dados de um Produto para exibicao na tela de Estoque (RF0005/RF0021):
// quantidade, percentual e cor de saude, alem dos alertas de minimo e validade.
// So formata o que o dominio ja decidiu; nenhuma regra de estoque e calculada aqui.
public class ProdutoEstoqueItem
{
    public required Produto Produto { get; init; }

    // Data de referencia dos textos de validade; padrao: hoje (os testes passam uma data fixa).
    public DateOnly Hoje { get; init; } = DateOnly.FromDateTime(DateTime.Today);

    public string Nome => Produto.Nome;

    public string Categoria => TextosDeEstoque.CategoriaComApresentacao(Produto);

    public int QuantidadeAtual => Produto.QuantidadeAtual;

    // O minimo so aparece quando foi definido: a planilha real do CEAN nao informa, e repetir "nao definido" em toda linha so polui.
    public bool MostrarMinimo => Produto.EstoqueMinimo is not null;
    public string MinimoTexto => TextosDeEstoque.Minimo(Produto);
    public string PercentualTexto => TextosDeEstoque.PercentualDoMinimo(Produto);

    public Color CorSaude => Produto.CorSaude == CorSaudeEstoque.Verde
        ? Color.FromArgb("#169B62")
        : Color.FromArgb("#D95757");

    public bool MostrarAlertaSemEstoque => Produto.EstaSemEstoque;
    public bool MostrarAlertaCritico => Produto.EstaCritico && !Produto.EstaSemEstoque;
    public bool MostrarAlertaVencido => Produto.TemLoteVencido(Hoje);
    public bool MostrarAlertaValidadeProxima =>
        Produto.TemValidadeProxima(Hoje, SigaCean.Services.EstoqueService.DiasAlertaValidade);

    // "vence em 12/03/2027 (41 dias)" ou "venceu em 01/09/2026 (há 38 dias)"; vazio quando nada em estoque tem validade.
    public bool MostrarValidade => Produto.ProximaValidade is not null;
    public string ValidadeTexto => Produto.ProximaValidade is { } data ? TextosDeEstoque.DescreverValidade(data, Hoje) : string.Empty;

    public Color CorValidade => Produto.ProximaValidade is { } data && data < Hoje
        ? Color.FromArgb("#D95757")
        : MostrarAlertaValidadeProxima ? Color.FromArgb("#F59B24") : Color.FromArgb("#718589");
}
