using SigaCean.Domain;
using SigaCean.Infrastructure;

namespace SigaCean.Services;

public enum OrdemRelatorio
{
    PorNome,
    MaiorConsumo
}

// UC08 (CONFIRMADO): agrega as movimentacoes do periodo por produto/tipo,
// sem nenhuma formula de "saude do estoque" (isso fica na tela de Estoque).
public class RelatorioService(DataStore dataStore)
{
    public IReadOnlyList<ItemRelatorioConsumo> GerarRelatorioConsumo(
        DateTime dataInicio, DateTime dataFim,
        CategoriaProduto? categoria = null, OrdemRelatorio ordem = OrdemRelatorio.PorNome)
    {
        var meses = PeriodoRelatorio.MesesNoPeriodo(dataInicio, dataFim);

        var itens = dataStore.Movimentacoes
            .Where(m => m.Status == StatusMovimentacao.Ativa) // canceladas/corrigidas tiveram o efeito desfeito
            .Where(m => m.Data >= dataInicio && m.Data <= dataFim)
            .Where(m => categoria is null || m.Produto.Categoria == categoria)
            .GroupBy(m => m.Produto)
            .Select(g =>
            {
                var saidas = g.OfType<SaidaEstoque>().Sum(s => s.Quantidade);
                return new ItemRelatorioConsumo
                {
                    Produto = g.Key,
                    TotalEntradas = g.OfType<EntradaEstoque>().Sum(e => e.Quantidade),
                    TotalSaidas = saidas,
                    TotalAjustes = g.OfType<AjusteBalanco>().Sum(a => a.Quantidade),
                    MediaMensalSaidas = Math.Round(saidas / meses, 1)
                };
            });

        return (ordem == OrdemRelatorio.MaiorConsumo
                ? itens.OrderByDescending(i => i.TotalSaidas).ThenBy(i => i.Produto.Nome)
                : itens.OrderBy(i => i.Produto.Nome))
            .ToList();
    }

    // Data da movimentacao mais recente (qualquer status); a tela usa para orientar quando o periodo escolhido esta vazio.
    public DateTime? DataDaUltimaMovimentacao() =>
        dataStore.Movimentacoes.Count == 0 ? null : dataStore.Movimentacoes.Max(m => m.Data);
}

public class ItemRelatorioConsumo
{
    public required Produto Produto { get; init; }
    public int TotalEntradas { get; init; }
    public int TotalSaidas { get; init; }
    public int TotalAjustes { get; init; }
    public decimal MediaMensalSaidas { get; init; } // saidas do periodo divididas pelos meses que ele cobre

    public int EstoqueAtual => Produto.QuantidadeAtual;
}
