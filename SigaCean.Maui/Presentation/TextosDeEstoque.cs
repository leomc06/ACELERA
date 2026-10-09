using SigaCean.Domain;

namespace SigaCean.Presentation;

// Textos de exibicao do estoque, definidos uma unica vez para a lista, a ficha do produto e o painel.
// So formata o que o dominio ja decidiu (sem dependencia de MAUI, por isso e testado direto).
public static class TextosDeEstoque
{
    // "vence em 12/03/2027 (41 dias)" ou "venceu em 01/09/2026 (há 38 dias)".
    public static string DescreverValidade(DateOnly data, DateOnly hoje)
    {
        var dias = data.DayNumber - hoje.DayNumber;
        return dias switch
        {
            < 0 => $"venceu em {data:dd/MM/yyyy} (há {-dias} {(dias == -1 ? "dia" : "dias")})",
            0 => $"vence hoje ({data:dd/MM/yyyy})",
            1 => $"vence amanhã ({data:dd/MM/yyyy})",
            _ => $"vence em {data:dd/MM/yyyy} ({dias} dias)"
        };
    }

    // Situacao de um saldo na ficha do produto: sem data de validade, ele sai por ultimo (FEFO).
    public static string DescreverSaldo(DateOnly? validade, DateOnly hoje) =>
        validade is { } data ? DescreverValidade(data, hoje) : "Sai por último";

    public static string CategoriaComApresentacao(Produto produto) =>
        string.IsNullOrWhiteSpace(produto.ApresentacaoEspecificacao)
            ? Rotulos.Categoria(produto.Categoria)
            : $"{Rotulos.Categoria(produto.Categoria)} · {produto.ApresentacaoEspecificacao}";

    public static string Minimo(Produto produto) => $"mínimo: {produto.EstoqueMinimo}";

    public static string PercentualDoMinimo(Produto produto) =>
        produto.PercentualSaude is { } p ? $"{p:0}% do mínimo" : string.Empty;
}
