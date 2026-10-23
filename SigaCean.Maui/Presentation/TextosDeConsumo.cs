using System.Globalization;
using SigaCean.Services;

namespace SigaCean.Presentation;

// Textos dos graficos e do "Relatorio por categoria", definidos uma unica vez para a tela, os cartoes e o PDF.
// So formata o que o RelatorioService ja calculou (sem dependencia de MAUI, por isso e testado direto).
public static class TextosDeConsumo
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    // "1.234": numero inteiro com separador de milhar.
    public static string Numero(double valor) => valor.ToString("N0", Cultura);

    // "25%" (ou "—" quando o total e zero e nao ha o que dividir).
    public static string Percentual(double valor, double total) =>
        total <= 0 ? "—"
        : valor > 0 && Math.Round(valor / total * 100) < 1 ? "<1%" // tem uso, mas menos de 1%: nao mostrar "0%"
        : $"{Math.Round(valor / total * 100):0}%";

    // "3 produto(s) · 120 un. em estoque · 45 un. usadas no periodo"
    public static string Resumo(ConsumoPorCategoria c) =>
        c.QuantidadeDeProdutos == 0
            ? "Nenhum produto cadastrado nesta categoria."
            : $"{c.QuantidadeDeProdutos} produto(s) · {Numero(c.EstoqueAtual)} un. em estoque · {Numero(c.TotalSaidas)} un. usadas no período";

    public static string MaisUsado(ConsumoPorCategoria c) =>
        c.MaisUsado is { } mais
            ? $"Mais usado: {mais.Produto.Nome} ({Numero(mais.TotalSaidas)} un.)"
            : "Nenhuma saída neste período.";

    // Vazio quando nao ha o que comparar (menos de dois produtos tiveram saida).
    public static string MenosUsado(ConsumoPorCategoria c) =>
        c.MenosUsado is { } menos
            ? $"Menos usado: {menos.Produto.Nome} ({Numero(menos.TotalSaidas)} un.)"
            : string.Empty;

    // Vazio quando todos os produtos da categoria tiveram saida (ou ela nao tem produtos).
    public static string SemSaida(ConsumoPorCategoria c) =>
        c.QuantidadeDeProdutos > 0 && c.ProdutosSemSaida > 0
            ? $"{c.ProdutosSemSaida} produto(s) sem nenhuma saída no período."
            : string.Empty;

    // Encurta nomes longos (os da planilha real trazem apresentacao e especificacao) para caberem no grafico.
    public static string Encurtar(string texto, int maximo) =>
        texto.Length <= maximo ? texto : texto[..(maximo - 1)].TrimEnd() + "…";
}
