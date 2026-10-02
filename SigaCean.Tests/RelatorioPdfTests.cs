using System.Text;
using SigaCean.Infrastructure;
using SigaCean.Presentation.Reports;
using SigaCean.Services;

namespace SigaCean.Tests;

public class RelatorioPdfTests
{
    private static string CaminhoTemporario() => Path.Combine(Path.GetTempPath(), $"relatorio_{Guid.NewGuid():N}.pdf");

    [Fact]
    public void Gerar_ComDadosReais_CriaUmPdfValido()
    {
        var loja = new DataStore();
        var itens = new RelatorioService(loja).GerarRelatorioConsumo(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        var caminho = CaminhoTemporario();

        try
        {
            RelatorioConsumoPdf.Gerar(caminho, itens, TipoPeriodo.Anual, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), "Todas as categorias", "Teste");

            var bytes = File.ReadAllBytes(caminho);
            Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
            Assert.True(bytes.Length > 5_000);
        }
        finally
        {
            File.Delete(caminho);
        }
    }

    [Fact]
    public void Gerar_SemMovimentacoes_AindaCriaOPdf()
    {
        var caminho = CaminhoTemporario();

        try
        {
            RelatorioConsumoPdf.Gerar(caminho, [], TipoPeriodo.Mensal, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), "Medicamento", null);

            Assert.True(new FileInfo(caminho).Length > 0);
        }
        finally
        {
            File.Delete(caminho);
        }
    }

    [Theory]
    [InlineData(TipoPeriodo.Mensal, "2026-01-01", "2026-01-31", "Relatorio_Consumo_CEAN_Mensal_2026-01.pdf")]
    [InlineData(TipoPeriodo.Trimestral, "2026-04-01", "2026-06-30", "Relatorio_Consumo_CEAN_Trimestral_2026-T2.pdf")]
    [InlineData(TipoPeriodo.Anual, "2026-01-01", "2026-12-31", "Relatorio_Consumo_CEAN_Anual_2026.pdf")]
    [InlineData(TipoPeriodo.Personalizado, "2026-03-05", "2026-03-20", "Relatorio_Consumo_CEAN_20260305_a_20260320.pdf")]
    public void NomeSugerido_TrazOTipoEOPeriodo(TipoPeriodo tipo, string inicio, string fim, string esperado)
    {
        Assert.Equal(esperado, RelatorioConsumoPdf.NomeSugerido(tipo, DateTime.Parse(inicio), DateTime.Parse(fim)));
    }
}
