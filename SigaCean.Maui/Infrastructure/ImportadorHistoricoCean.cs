using System.Globalization;
using System.Text.RegularExpressions;
using SigaCean.Domain;

namespace SigaCean.Infrastructure;

// Transforma as linhas da aba ATIVIDADE da planilha do CEAN em movimentacoes do sistema.
// O historico e so registro: o estoque atual vem da aba "ESTOQUE DE MEDICAMENTOS" e nao e alterado aqui.
public static class ImportadorHistoricoCean
{
    // Nomes escritos de forma diferente na aba ATIVIDADE (erro de digitacao) -> nome da aba de estoque.
    // So entram aqui os casos em que juntar os nomes nao afasta o historico do estoque atual da planilha.
    // Outros nomes parecidos (ex.: "SULFATO DE MORFINA ... - AMPOLA", "ADVOCATE, IMIDACLOPRIDA ...") tem movimentacoes
    // proprias que, somadas ao produto principal, passariam do estoque real; por isso ficam sem importar.
    public static readonly IReadOnlyDictionary<string, string> Equivalencias = new Dictionary<string, string>
    {
        ["MELOXICAM - 0,5 MG - CAPSULA"] = "MELOXICAN - 0,5 MG - CAPSULA",
        ["AURITOP, GEO OTOLÓGICO 30 ML"] = "AURITOP, GEL OTOLÓGICO 30 ML",
        ["AFOXOLANER, NEXGARD 136 MG - 25/50 KG - CAIXA COM 3 COMP"] = "NEXGARD, AFOXOLANER 136 MG - 25/50 KG - CAIXA COM 3 COMP"
    };

    public static ResultadoImportacao Importar(IReadOnlyList<Produto> produtos, IEnumerable<LinhaHistoricoCean> linhas)
    {
        var porNome = produtos.ToDictionary(p => p.Nome);
        var saldoDoHistorico = new Dictionary<Produto, int>();
        var movimentacoes = new List<Movimentacao>();
        var semProduto = new List<LinhaHistoricoCean>();

        foreach (var linha in linhas)
        {
            var nome = Equivalencias.GetValueOrDefault(linha.Produto, linha.Produto);
            if (!porNome.TryGetValue(nome, out var produto))
            {
                semProduto.Add(linha);
                continue;
            }

            var data = DateOnly.ParseExact(linha.Data, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToDateTime(TimeOnly.MinValue);
            var saldo = saldoDoHistorico.GetValueOrDefault(produto);
            var observacao = linha.Observacao.Trim();

            Movimentacao movimentacao;
            if (observacao.StartsWith("BALAN", StringComparison.OrdinalIgnoreCase))
            {
                // "BALANCO" na planilha e a correcao apos conferencia fisica (RD0023): vira Ajuste de Balanco.
                var diferenca = linha.Entrada ? linha.Quantidade : -linha.Quantidade;
                var ajuste = new AjusteBalanco
                {
                    Produto = produto,
                    Quantidade = diferenca,
                    QuantidadeAnterior = saldo,
                    QuantidadeApurada = saldo + diferenca,
                    Data = data
                };
                if (diferenca < 0) ajuste.LotesConsumidos.Add(new LoteConsumido(null, -diferenca));
                movimentacao = ajuste;
                saldo += diferenca;
            }
            else if (linha.Entrada)
            {
                movimentacao = CriarEntrada(produto, linha, data, observacao);
                saldo += linha.Quantidade;
            }
            else
            {
                var saida = new SaidaEstoque
                {
                    Produto = produto,
                    Quantidade = linha.Quantidade,
                    Destino = observacao.Length > 0 ? observacao : "Não informado",
                    Data = data
                };
                saida.LotesConsumidos.Add(new LoteConsumido(null, linha.Quantidade));
                movimentacao = saida;
                saldo -= linha.Quantidade;
            }

            saldoDoHistorico[produto] = saldo;
            movimentacoes.Add(movimentacao);
        }

        return new ResultadoImportacao(movimentacoes, semProduto, saldoDoHistorico);
    }

    private static EntradaEstoque CriarEntrada(Produto produto, LinhaHistoricoCean linha, DateTime data, string observacao)
    {
        TipoOrigemEntrada? tipo = null;
        string? numeroAF = null;
        string? detalhe = null;

        if (observacao.StartsWith("COMPRA", StringComparison.OrdinalIgnoreCase))
        {
            tipo = TipoOrigemEntrada.Compra;
            var af = Regex.Match(observacao, @"\bAF\s+(\d+/\d+)", RegexOptions.IgnoreCase);
            if (af.Success) numeroAF = af.Groups[1].Value;
            else detalhe = DepoisDoPontoEVirgula(observacao);
        }
        else if (observacao.StartsWith("DOA", StringComparison.OrdinalIgnoreCase))
        {
            tipo = TipoOrigemEntrada.Doacao;
            detalhe = DepoisDoPontoEVirgula(observacao);
        }
        else if (observacao.Length > 0)
        {
            detalhe = observacao; // ex.: "SANTA CASA" — a planilha nao diz se foi compra, concessao ou doacao
        }

        return new EntradaEstoque
        {
            Produto = produto,
            Quantidade = linha.Quantidade,
            Data = data,
            TipoOrigem = tipo,
            NumeroAF = numeroAF,
            DetalheOrigem = detalhe
        };
    }

    private static string? DepoisDoPontoEVirgula(string texto)
    {
        var indice = texto.IndexOf(';');
        if (indice < 0) return null;
        var resto = texto[(indice + 1)..].Trim();
        return resto.Length > 0 ? resto : null;
    }
}

public record ResultadoImportacao(
    IReadOnlyList<Movimentacao> Movimentacoes,
    IReadOnlyList<LinhaHistoricoCean> SemProduto,
    IReadOnlyDictionary<Produto, int> SaldoDoHistorico);
