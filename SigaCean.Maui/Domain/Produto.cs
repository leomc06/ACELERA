namespace SigaCean.Domain;

public class Produto
{
    // Percentual do estoque minimo abaixo do qual o alerta deixa de ser so "baixo" e vira "critico"
    // (pedido do time: "alertas tambem para algum produto que esteja bem abaixo"). Ajustavel.
    private const double LimiarCriticoPercentual = 0.5;

    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Nome { get; set; }
    public string? ApresentacaoEspecificacao { get; set; } // relevante sobretudo para medicamentos
    public required CategoriaProduto Categoria { get; set; }
    public int? EstoqueMinimo { get; set; }
    public bool Ativo { get; set; } = true;

    public List<LoteEstoque> Lotes { get; } = new();

    public int QuantidadeAtual => Lotes.Sum(l => l.Quantidade);

    public double? PercentualSaude =>
        EstoqueMinimo is null or 0 ? null : Math.Round(QuantidadeAtual / (double)EstoqueMinimo.Value * 100, 0);

    public CorSaudeEstoque CorSaude =>
        EstoqueMinimo is null ? CorSaudeEstoque.Verde
        : QuantidadeAtual <= EstoqueMinimo.Value ? CorSaudeEstoque.Vermelho
        : CorSaudeEstoque.Verde;

    public bool EstaAbaixoDoMinimo => EstoqueMinimo is not null && QuantidadeAtual <= EstoqueMinimo.Value;

    public bool EstaCritico => EstoqueMinimo is not null && QuantidadeAtual <= EstoqueMinimo.Value * LimiarCriticoPercentual;

    public bool TemLoteVencido(DateOnly hoje) => Lotes.Any(l => l.EstaVencido(hoje));

    public bool TemLoteProximoDoVencimento(DateOnly hoje, int diasAlerta) =>
        Lotes.Any(l => l.EstaProximoDoVencimento(hoje, diasAlerta));

    // Consome quantidade pelas regras FEFO (first-expire, first-out): lotes sem validade
    // conhecida sao tratados como "vence por ultimo" para nao mascarar lotes com data real.
    public IReadOnlyList<LoteConsumido> ConsumirQuantidade(int quantidade)
    {
        if (quantidade > QuantidadeAtual)
            throw new InvalidOperationException($"Quantidade solicitada ({quantidade}) maior que a disponivel ({QuantidadeAtual}) para '{Nome}'.");

        var consumidos = new List<LoteConsumido>();
        var restante = quantidade;
        foreach (var lote in Lotes.OrderBy(l => l.DataValidade ?? DateOnly.MaxValue))
        {
            if (restante <= 0) break;
            var consumidoDoLote = Math.Min(lote.Quantidade, restante);
            if (consumidoDoLote > 0)
                consumidos.Add(new LoteConsumido(lote.Codigo, lote.DataValidade, consumidoDoLote));
            lote.Quantidade -= consumidoDoLote;
            restante -= consumidoDoLote;
        }

        Lotes.RemoveAll(l => l.Quantidade == 0 && l.Codigo is not null);
        return consumidos;
    }

    // Desfaz uma entrada: retira a quantidade do lote em que ela entrou. Falha se esse estoque ja foi usado.
    public void RemoverDoLote(string? codigo, DateOnly? dataValidade, int quantidade)
    {
        var lote = Lotes.FirstOrDefault(l =>
            l.Quantidade >= quantidade &&
            (codigo is not null ? l.Codigo == codigo : l.Codigo is null && l.DataValidade == dataValidade));

        if (lote is null)
            throw new InvalidOperationException(
                $"Nao e possivel desfazer: parte das {quantidade} unidades de '{Nome}' dessa entrada ja foi utilizada.");

        lote.Quantidade -= quantidade;
        Lotes.RemoveAll(l => l.Quantidade == 0);
    }

    // Desfaz uma saida (ou ajuste para baixo): devolve cada parcela ao lote de onde saiu.
    public void DevolverLotes(IEnumerable<LoteConsumido> lotes)
    {
        foreach (var l in lotes)
            AdicionarQuantidade(l.Quantidade, l.Codigo, l.DataValidade);
    }

    public void AdicionarQuantidade(int quantidade, string? lote, DateOnly? dataValidade)
    {
        var loteExistente = lote is not null ? Lotes.FirstOrDefault(l => l.Codigo == lote) : null;
        if (loteExistente is not null)
        {
            loteExistente.Quantidade += quantidade;
            return;
        }

        Lotes.Add(new LoteEstoque { Codigo = lote, DataValidade = dataValidade, Quantidade = quantidade });
    }
}
