using SigaCean.Domain;
using SigaCean.Infrastructure;

namespace SigaCean.Services;

public class EstoqueService(DataStore dataStore)
{
    // Janela de alerta de validade: 1 mes, conforme decidido pelo time (ajustavel).
    public const int DiasAlertaValidade = 30;

    public IReadOnlyList<Produto> ObterTodosComSituacao() => dataStore.Produtos.Where(p => p.Ativo).ToList();

    // Pesquisa por nome e/ou categoria (RF0004), opcionalmente restrita a uma situacao de alerta.
    public IReadOnlyList<Produto> Filtrar(string? nome, CategoriaProduto? categoria, SituacaoEstoque situacao, DateOnly? hoje = null)
    {
        var dia = hoje ?? DateOnly.FromDateTime(DateTime.Today);
        var termo = nome?.Trim();

        return dataStore.Produtos
            .Where(p => p.Ativo)
            .Where(p => string.IsNullOrEmpty(termo) || p.Nome.Contains(termo, StringComparison.CurrentCultureIgnoreCase))
            .Where(p => categoria is null || p.Categoria == categoria)
            .Where(p => Atende(p, situacao, dia))
            .OrderBy(p => p.Nome)
            .ToList();
    }

    public int Contar(SituacaoEstoque situacao, DateOnly? hoje = null)
    {
        var dia = hoje ?? DateOnly.FromDateTime(DateTime.Today);
        return dataStore.Produtos.Count(p => p.Ativo && Atende(p, situacao, dia));
    }

    private static bool Atende(Produto p, SituacaoEstoque situacao, DateOnly hoje) => situacao switch
    {
        SituacaoEstoque.Critico => p.EstaCritico,
        SituacaoEstoque.AbaixoDoMinimo => p.EstaAbaixoDoMinimo,
        SituacaoEstoque.Vencido => p.TemLoteVencido(hoje),
        // Igual ao alerta mostrado em cada item: vencido tem prioridade sobre "validade proxima".
        SituacaoEstoque.ValidadeProxima => !p.TemLoteVencido(hoje) && p.TemLoteProximoDoVencimento(hoje, DiasAlertaValidade),
        _ => true
    };

    public IReadOnlyList<Produto> ObterAbaixoDoMinimo() =>
        dataStore.Produtos.Where(p => p.Ativo && p.EstaAbaixoDoMinimo).ToList();

    public IReadOnlyList<Produto> ObterCriticos() =>
        dataStore.Produtos.Where(p => p.Ativo && p.EstaCritico).ToList();

    public IReadOnlyList<Produto> ObterVencidos(DateOnly? hoje = null) =>
        dataStore.Produtos.Where(p => p.Ativo && p.TemLoteVencido(hoje ?? DateOnly.FromDateTime(DateTime.Today))).ToList();

    public IReadOnlyList<Produto> ObterProximosDoVencimento(DateOnly? hoje = null, int diasAlerta = DiasAlertaValidade) =>
        dataStore.Produtos
            .Where(p => p.Ativo && p.TemLoteProximoDoVencimento(hoje ?? DateOnly.FromDateTime(DateTime.Today), diasAlerta))
            .ToList();
}
