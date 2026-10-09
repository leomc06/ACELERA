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

    // Zerado e zerado, haja ou nao estoque minimo definido (a planilha do CEAN nao informa o minimo).
    public bool EstaSemEstoque => QuantidadeAtual == 0;

    public CorSaudeEstoque CorSaude =>
        EstaSemEstoque ? CorSaudeEstoque.Vermelho
        : EstoqueMinimo is null ? CorSaudeEstoque.Verde
        : QuantidadeAtual <= EstoqueMinimo.Value ? CorSaudeEstoque.Vermelho
        : CorSaudeEstoque.Verde;

    public bool EstaAbaixoDoMinimo => EstoqueMinimo is not null && QuantidadeAtual <= EstoqueMinimo.Value;

    public bool EstaCritico => EstoqueMinimo is not null && QuantidadeAtual <= EstoqueMinimo.Value * LimiarCriticoPercentual;

    // Validade so conta para o que ainda esta em estoque: um lote ja zerado nao deixa o produto "vencido".
    public bool TemLoteVencido(DateOnly hoje) => Lotes.Any(l => l.Quantidade > 0 && l.EstaVencido(hoje));

    public bool TemLoteProximoDoVencimento(DateOnly hoje, int diasAlerta) =>
        Lotes.Any(l => l.Quantidade > 0 && l.EstaProximoDoVencimento(hoje, diasAlerta));

    // Regra unica do alerta "validade proxima" (lista, filtro e contadores usam esta): quem ja tem lote vencido
    // aparece como vencido, e nao tambem como "proximo".
    public bool TemValidadeProxima(DateOnly hoje, int diasAlerta) =>
        !TemLoteVencido(hoje) && TemLoteProximoDoVencimento(hoje, diasAlerta);

    // Unico lugar com a regra FEFO (first-expire, first-out): o que vence primeiro sai primeiro, e o estoque sem
    // validade conhecida e tratado como "vence por ultimo" para nao mascarar o que tem data real.
    // Nao altera nada: so diz de quais validades a quantidade sairia.
    public IReadOnlyList<LoteConsumido> PrevisaoFefo(int quantidade)
    {
        if (quantidade > QuantidadeAtual)
            throw new InvalidOperationException($"Quantidade solicitada ({quantidade}) maior que a disponível ({QuantidadeAtual}) para {Nome}.");

        var previsao = new List<LoteConsumido>();
        var restante = quantidade;
        foreach (var lote in Lotes.Where(l => l.Quantidade > 0).OrderBy(l => l.DataValidade ?? DateOnly.MaxValue))
        {
            if (restante <= 0) break;
            var daqui = Math.Min(lote.Quantidade, restante);
            previsao.Add(new LoteConsumido(lote.DataValidade, daqui));
            restante -= daqui;
        }

        return previsao;
    }

    // Consome a quantidade seguindo a previsao FEFO e devolve o que saiu de cada validade.
    public IReadOnlyList<LoteConsumido> ConsumirQuantidade(int quantidade)
    {
        var consumidos = PrevisaoFefo(quantidade);
        foreach (var parcela in consumidos)
            Lotes.First(l => l.DataValidade == parcela.DataValidade).Quantidade -= parcela.Quantidade;

        Lotes.RemoveAll(l => l.Quantidade == 0);
        return consumidos;
    }

    // Validade mais proxima entre o que ainda existe em estoque (nulo se nada tem validade informada).
    public DateOnly? ProximaValidade =>
        Lotes.Where(l => l.Quantidade > 0 && l.DataValidade is not null).Min(l => l.DataValidade);

    // Desfaz uma entrada: retira a quantidade do estoque que tem aquela validade. Falha se esse estoque ja foi usado.
    public void RemoverDoLote(DateOnly? dataValidade, int quantidade)
    {
        var lote = Lotes.FirstOrDefault(l => l.Quantidade >= quantidade && l.DataValidade == dataValidade);

        if (lote is null)
            throw new InvalidOperationException(
                $"Não é possível desfazer: parte das {quantidade} unidades de '{Nome}' dessa entrada já foi utilizada.");

        lote.Quantidade -= quantidade;
        Lotes.RemoveAll(l => l.Quantidade == 0);
    }

    // Desfaz uma saida (ou ajuste para baixo): devolve cada parcela ao estoque da validade de onde saiu.
    public void DevolverLotes(IEnumerable<LoteConsumido> lotes)
    {
        foreach (var l in lotes)
            AdicionarQuantidade(l.Quantidade, l.DataValidade);
    }

    // Entradas com a mesma validade (ou ambas sem validade) somam no mesmo saldo.
    public void AdicionarQuantidade(int quantidade, DateOnly? dataValidade)
    {
        var existente = Lotes.FirstOrDefault(l => l.DataValidade == dataValidade);
        if (existente is not null)
        {
            existente.Quantidade += quantidade;
            return;
        }

        Lotes.Add(new LoteEstoque { DataValidade = dataValidade, Quantidade = quantidade });
    }
}
