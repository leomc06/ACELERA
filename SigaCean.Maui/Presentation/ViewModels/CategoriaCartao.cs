using SigaCean.Services;

namespace SigaCean.Presentation.ViewModels;

// Cartao de uma categoria: quem foi o mais e o menos usado dentro dela. So formata o que o RelatorioService calculou.
public class CategoriaCartao
{
    public CategoriaCartao(ConsumoPorCategoria consumo, int totalGeralDeSaidas)
    {
        Cor = PaletaDeCategorias.Cor(consumo.Categoria);
        Nome = Rotulos.Categoria(consumo.Categoria);
        Resumo = TextosDeConsumo.Resumo(consumo);
        MaisUsadoTexto = TextosDeConsumo.MaisUsado(consumo);
        MenosUsadoTexto = TextosDeConsumo.MenosUsado(consumo);
        SemSaidaTexto = TextosDeConsumo.SemSaida(consumo);
        ParticipacaoTexto = consumo.TotalSaidas > 0 ? $"{TextosDeConsumo.Percentual(consumo.TotalSaidas, totalGeralDeSaidas)} do uso total" : string.Empty;
    }

    public Color Cor { get; }
    public string Nome { get; }
    public string Resumo { get; }
    public string MaisUsadoTexto { get; }
    public string MenosUsadoTexto { get; }
    public bool MostrarMenosUsado => MenosUsadoTexto.Length > 0;
    public string SemSaidaTexto { get; }
    public bool MostrarSemSaida => SemSaidaTexto.Length > 0;
    public string ParticipacaoTexto { get; }
    public bool MostrarParticipacao => ParticipacaoTexto.Length > 0;
}
