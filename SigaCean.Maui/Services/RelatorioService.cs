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

    // Uma linha para CADA categoria (mesmo as sem produto ou sem uso): estoque atual, saidas no periodo e qual produto
    // foi o mais e o menos usado dentro dela. Alimenta os graficos e o "Relatorio por categoria".
    // Mesmas regras do relatorio por produto: so conta movimentacao Ativa (cancelada e corrigida ja tiveram o efeito desfeito).
    public IReadOnlyList<ConsumoPorCategoria> GerarConsumoPorCategoria(DateTime dataInicio, DateTime dataFim)
    {
        var saidasPorProduto = dataStore.Movimentacoes
            .OfType<SaidaEstoque>()
            .Where(m => m.Status == StatusMovimentacao.Ativa)
            .Where(m => m.Data >= dataInicio && m.Data <= dataFim)
            .GroupBy(m => m.Produto)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.Quantidade));

        var ativos = dataStore.Produtos.Where(p => p.Ativo).ToList();

        return Enum.GetValues<CategoriaProduto>().Select(categoria =>
        {
            var produtos = ativos
                .Where(p => p.Categoria == categoria)
                .Select(p => new ConsumoDeProduto(p, saidasPorProduto.GetValueOrDefault(p)))
                .ToList();

            var usados = produtos
                .Where(c => c.TotalSaidas > 0)
                .OrderByDescending(c => c.TotalSaidas)
                .ThenBy(c => c.Produto.Nome)
                .ToList();

            return new ConsumoPorCategoria
            {
                Categoria = categoria,
                QuantidadeDeProdutos = produtos.Count,
                EstoqueAtual = produtos.Sum(c => c.Produto.QuantidadeAtual),
                TotalSaidas = usados.Sum(c => c.TotalSaidas),
                MaisUsado = usados.FirstOrDefault(),
                MenosUsado = usados.Count > 1 ? usados[^1] : null,
                ProdutosSemSaida = produtos.Count - usados.Count,
                ProdutosUsados = usados
            };
        }).ToList();
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

// Quanto de um produto saiu no periodo (so saidas ativas).
public record ConsumoDeProduto(Produto Produto, int TotalSaidas);

// Uma linha do "Consumo por categoria": como uma categoria (Medicamento, Alimentar...) esta e foi usada.
// "Uso" = saidas ativas no periodo; entradas e ajustes de balanco nao contam como uso.
public class ConsumoPorCategoria
{
    public required CategoriaProduto Categoria { get; init; }

    public int QuantidadeDeProdutos { get; init; } // produtos ativos da categoria
    public int EstoqueAtual { get; init; } // soma do que ha em estoque hoje
    public int TotalSaidas { get; init; } // soma das saidas ativas no periodo

    // Produto com mais saidas no periodo. Nulo se nada saiu.
    public ConsumoDeProduto? MaisUsado { get; init; }

    // Produto com menos saidas ENTRE OS QUE TIVERAM SAIDA. Nulo se menos de dois produtos tiveram saida
    // (com um so nao ha com quem comparar; os que nao tiveram nenhuma aparecem em ProdutosSemSaida).
    public ConsumoDeProduto? MenosUsado { get; init; }

    public int ProdutosSemSaida { get; init; } // produtos ativos da categoria sem nenhuma saida no periodo

    // Os produtos que tiveram saida, do mais para o menos usado (empate: por nome).
    public IReadOnlyList<ConsumoDeProduto> ProdutosUsados { get; init; } = [];
}
