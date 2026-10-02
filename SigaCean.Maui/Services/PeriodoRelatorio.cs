namespace SigaCean.Services;

public enum TipoPeriodo
{
    Mensal,
    Trimestral,
    Anual,
    Personalizado
}

// Calcula os periodos fechados dos relatorios do CEAN (mensal, trimestral e anual).
public static class PeriodoRelatorio
{
    // Periodo que contem a data de referencia: mes, trimestre ou ano civil.
    public static (DateTime Inicio, DateTime Fim) Contendo(TipoPeriodo tipo, DateTime referencia) => tipo switch
    {
        TipoPeriodo.Mensal => (new DateTime(referencia.Year, referencia.Month, 1), UltimoDiaDoMes(referencia.Year, referencia.Month)),
        TipoPeriodo.Trimestral => TrimestreContendo(referencia),
        TipoPeriodo.Anual => (new DateTime(referencia.Year, 1, 1), new DateTime(referencia.Year, 12, 31)),
        _ => throw new ArgumentException("O periodo personalizado e escolhido pelas datas.", nameof(tipo))
    };

    // Move o periodo para tras (-1) ou para frente (+1), mantendo o mesmo tipo.
    public static (DateTime Inicio, DateTime Fim) Deslocar(TipoPeriodo tipo, DateTime inicioAtual, int passos)
    {
        var meses = tipo switch
        {
            TipoPeriodo.Mensal => 1,
            TipoPeriodo.Trimestral => 3,
            TipoPeriodo.Anual => 12,
            _ => throw new ArgumentException("O periodo personalizado nao tem setas.", nameof(tipo))
        };
        return Contendo(tipo, inicioAtual.AddMonths(meses * passos));
    }

    public static string Descrever(TipoPeriodo tipo, DateTime inicio, DateTime fim) => tipo switch
    {
        TipoPeriodo.Mensal => $"{NomeDoMes(inicio.Month)} de {inicio.Year}",
        TipoPeriodo.Trimestral => $"{(inicio.Month - 1) / 3 + 1}º trimestre de {inicio.Year} ({NomeDoMes(inicio.Month)} a {NomeDoMes(fim.Month)})",
        TipoPeriodo.Anual => inicio.Year.ToString(),
        _ => $"{inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}"
    };

    // Quantos "meses" o periodo representa, para calcular a media mensal (minimo de um mes).
    public static decimal MesesNoPeriodo(DateTime inicio, DateTime fim) =>
        Math.Max(1m, (decimal)((fim.Date - inicio.Date).TotalDays + 1) / 30.4375m);

    private static (DateTime, DateTime) TrimestreContendo(DateTime referencia)
    {
        var primeiroMes = (referencia.Month - 1) / 3 * 3 + 1;
        return (new DateTime(referencia.Year, primeiroMes, 1), UltimoDiaDoMes(referencia.Year, primeiroMes + 2));
    }

    private static DateTime UltimoDiaDoMes(int ano, int mes) => new(ano, mes, DateTime.DaysInMonth(ano, mes));

    private static string NomeDoMes(int mes) => mes switch
    {
        1 => "janeiro", 2 => "fevereiro", 3 => "março", 4 => "abril", 5 => "maio", 6 => "junho",
        7 => "julho", 8 => "agosto", 9 => "setembro", 10 => "outubro", 11 => "novembro", _ => "dezembro"
    };
}
