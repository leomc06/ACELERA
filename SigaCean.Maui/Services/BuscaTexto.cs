using System.Globalization;

namespace SigaCean.Services;

// Regra unica das pesquisas por texto: ignora maiusculas e acentos, entao "injetavel" acha "INJETÁVEL"
// e "lidocaina" acha "LIDOCAÍNA". Termo vazio encontra tudo.
public static class BuscaTexto
{
    private const CompareOptions Opcoes = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    public static bool Contem(string? texto, string? termo)
    {
        var procurado = termo?.Trim();
        if (string.IsNullOrEmpty(procurado)) return true;
        if (string.IsNullOrEmpty(texto)) return false;

        return CultureInfo.InvariantCulture.CompareInfo.IndexOf(texto, procurado, Opcoes) >= 0;
    }
}
