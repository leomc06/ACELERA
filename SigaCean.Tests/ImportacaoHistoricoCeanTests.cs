using SigaCean.Domain;
using SigaCean.Infrastructure;
using SigaCean.Services;

namespace SigaCean.Tests;

public class ImportacaoHistoricoCeanTests
{
    private static Produto NovoProduto(string nome = "PRODUTO X") =>
        new() { Nome = nome, Categoria = CategoriaProduto.Medicamento };

    private static LinhaHistoricoCean Linha(bool entrada, int qtd, string obs, string data = "2026-03-10", string produto = "PRODUTO X") =>
        new(data, entrada, produto, qtd, obs);

    private static T Unica<T>(ResultadoImportacao r) where T : Movimentacao =>
        Assert.IsType<T>(Assert.Single(r.Movimentacoes));

    // ---- Regras de interpretação (linhas sintéticas) ----

    [Fact]
    public void Balanco_ViraAjusteDeBalanco_ComSaldoAnteriorEApurado()
    {
        var p = NovoProduto();
        var r = ImportadorHistoricoCean.Importar([p],
        [
            Linha(true, 100, "BALANÇO"),
            Linha(false, 30, "BALANÇO"),
            Linha(true, 0, "BALANÇO")
        ]);

        var ajustes = r.Movimentacoes.Cast<AjusteBalanco>().ToList();
        Assert.Equal((0, 100, 100), (ajustes[0].QuantidadeAnterior, ajustes[0].QuantidadeApurada, ajustes[0].Quantidade));
        Assert.Equal((100, 70, -30), (ajustes[1].QuantidadeAnterior, ajustes[1].QuantidadeApurada, ajustes[1].Quantidade));
        Assert.Equal((70, 70, 0), (ajustes[2].QuantidadeAnterior, ajustes[2].QuantidadeApurada, ajustes[2].Quantidade));
        Assert.Equal(70, r.SaldoDoHistorico[p]);
    }

    [Fact]
    public void EntradaDeCompra_GuardaTipoENumeroDaAF()
    {
        var r = ImportadorHistoricoCean.Importar([NovoProduto()], [Linha(true, 5, "COMPRA/LICITAÇÃO; AF 002000/2025")]);

        var e = Unica<EntradaEstoque>(r);
        Assert.Equal(TipoOrigemEntrada.Compra, e.TipoOrigem);
        Assert.Equal("002000/2025", e.NumeroAF);
        Assert.Null(e.DetalheOrigem);
    }

    [Fact]
    public void EntradaDeCompra_ComAFNaoPadrao_GuardaOTextoComoDetalhe()
    {
        var r = ImportadorHistoricoCean.Importar([NovoProduto()], [Linha(true, 5, "COMPRA/LICITAÇÃO; AF, ALC/2026")]);

        var e = Unica<EntradaEstoque>(r);
        Assert.Equal(TipoOrigemEntrada.Compra, e.TipoOrigem);
        Assert.Null(e.NumeroAF);
        Assert.Equal("AF, ALC/2026", e.DetalheOrigem);
    }

    [Theory]
    [InlineData("DOAÇÃO", null)]
    [InlineData("DOAÇÃO; DR WAGNER", "DR WAGNER")]
    public void EntradaDeDoacao_GuardaTipoEDoador(string obs, string? doador)
    {
        var e = Unica<EntradaEstoque>(ImportadorHistoricoCean.Importar([NovoProduto()], [Linha(true, 5, obs)]));

        Assert.Equal(TipoOrigemEntrada.Doacao, e.TipoOrigem);
        Assert.Equal(doador, e.DetalheOrigem);
    }

    [Fact]
    public void EntradaSemOrigemConhecida_FicaSemTipo_NaoInventaCompraNemDoacao()
    {
        var r = ImportadorHistoricoCean.Importar([NovoProduto()],
        [
            Linha(true, 5, ""),
            Linha(true, 5, "SANTA CASA")
        ]);

        var entradas = r.Movimentacoes.Cast<EntradaEstoque>().ToList();
        Assert.All(entradas, e => Assert.Null(e.TipoOrigem));
        Assert.Null(entradas[0].DetalheOrigem);
        Assert.Equal("SANTA CASA", entradas[1].DetalheOrigem);
    }

    [Theory]
    [InlineData("CEAN; CANIL", "CEAN; CANIL")]
    [InlineData("", "Não informado")]
    [InlineData("DOAÇÃO PARA MUNICIPE", "DOAÇÃO PARA MUNICIPE")]
    public void Saida_GuardaODestino_OuNaoInformado(string obs, string destinoEsperado)
    {
        var s = Unica<SaidaEstoque>(ImportadorHistoricoCean.Importar([NovoProduto()], [Linha(false, 3, obs)]));

        Assert.Equal(destinoEsperado, s.Destino);
        Assert.Equal(3, s.Quantidade);
    }

    [Fact]
    public void Importar_NaoAlteraOEstoqueDosProdutos()
    {
        var p = NovoProduto();
        p.AdicionarQuantidade(10, null, null);

        ImportadorHistoricoCean.Importar([p], [Linha(true, 500, "BALANÇO"), Linha(false, 4, "CEAN; CANIL")]);

        Assert.Equal(10, p.QuantidadeAtual);
    }

    [Fact]
    public void Importar_GuardaDataDaPlanilha_ESemUsuarioResponsavel()
    {
        var s = Unica<SaidaEstoque>(ImportadorHistoricoCean.Importar([NovoProduto()], [Linha(false, 1, "", data: "2026-07-21")]));

        Assert.Equal(new DateTime(2026, 7, 21), s.Data);
        Assert.Null(s.UsuarioResponsavel);
        Assert.Equal(StatusMovimentacao.Ativa, s.Status);
    }

    [Fact]
    public void ProdutoQueNaoExiste_VaiParaSemProduto_ENaoQuebra()
    {
        var r = ImportadorHistoricoCean.Importar([NovoProduto()], [Linha(false, 1, "", produto: "NAO EXISTE")]);

        Assert.Empty(r.Movimentacoes);
        Assert.Single(r.SemProduto);
    }

    [Fact]
    public void NomeComErroDeDigitacao_UsaAEquivalenciaConhecida()
    {
        var p = NovoProduto("AURITOP, GEL OTOLÓGICO 30 ML");

        var r = ImportadorHistoricoCean.Importar([p], [Linha(false, 1, "", produto: "AURITOP, GEO OTOLÓGICO 30 ML")]);

        Assert.Same(p, Assert.Single(r.Movimentacoes).Produto);
    }

    // ---- Dados reais da planilha ----

    [Fact]
    public void Equivalencias_ApontamDeUmNomeDoHistoricoParaUmProdutoQueExiste()
    {
        var nomesDoHistorico = DadosCean.Historico.Select(l => l.Produto).ToHashSet();
        var nomesDeProdutos = DadosCean.Medicamentos.Select(m => m.Nome).ToHashSet();

        foreach (var (origem, destino) in ImportadorHistoricoCean.Equivalencias)
        {
            Assert.Contains(origem, nomesDoHistorico);
            Assert.Contains(destino, nomesDeProdutos);
        }
    }

    [Fact]
    public void HistoricoReal_NomesSemProdutoCorrespondente_SaoOsNoveConhecidos()
    {
        var r = ImportadorHistoricoCean.Importar(new DataStore().Produtos, DadosCean.Historico);

        var semProduto = r.SemProduto.Select(l => l.Produto).Distinct().Order().ToList();
        Assert.Equal(9, semProduto.Count);
        Assert.Equal(24, r.SemProduto.Count);
        Assert.Contains("XILAZINA", semProduto);
        Assert.Contains("SULFATO DE MORFINA 10 MG - INJETÁVEL - AMPOLA", semProduto);
        Assert.Contains(semProduto, n => n.StartsWith("DEXAMETASONA 200 MG, DEXAFLAN"));
        Assert.Contains(semProduto, n => n.StartsWith("AMOXICILINA TRIHIDRATADA, AGEMOXI - 17 MG"));
        Assert.Contains(semProduto, n => n.StartsWith("CARPROFENO, IBASA"));
        Assert.Contains(semProduto, n => n.StartsWith("ADVOCATE, IMIDACLOPRIDA"));
        Assert.Equal(DadosCean.Historico.Length, r.Movimentacoes.Count + r.SemProduto.Count);
    }

    [Fact]
    public void HistoricoReal_SeteProdutosTemSaldoDiferenteDoEstoqueDaPlanilha()
    {
        var loja = new DataStore();
        var r = ImportadorHistoricoCean.Importar(loja.Produtos, DadosCean.Historico);

        var divergentes = loja.Produtos
            .Where(p => r.SaldoDoHistorico.GetValueOrDefault(p) != p.QuantidadeAtual)
            .Select(p => p.Nome)
            .ToList();

        Assert.Equal(7, divergentes.Count);
        Assert.Contains("CLORETO DE POTÁSSIO 19,1% - INJETÁVEL, AMPOLA", divergentes);
        Assert.Contains("TACROLIMO, TARFIC 0,1", divergentes);
    }

    [Fact]
    public void DataStore_CarregaOHistoricoSemMexerNoEstoque()
    {
        var loja = new DataStore();

        Assert.Equal(EsperadoImportadas, loja.Movimentacoes.Count);
        Assert.Equal(4878, loja.Produtos.Sum(p => p.QuantidadeAtual));
        Assert.All(loja.Movimentacoes, m => Assert.Equal(StatusMovimentacao.Ativa, m.Status));
    }

    [Fact]
    public void HistoricoReal_TiposDeMovimentacao()
    {
        var loja = new DataStore();

        Assert.Equal(EsperadoAjustes, loja.Movimentacoes.OfType<AjusteBalanco>().Count());
        Assert.Equal(EsperadoEntradas, loja.Movimentacoes.OfType<EntradaEstoque>().Count());
        Assert.Equal(EsperadoSaidas, loja.Movimentacoes.OfType<SaidaEstoque>().Count());
    }

    [Fact]
    public void HistoricoReal_PrimeiraMovimentacao_EOBalancoInicialDoPrador()
    {
        var loja = new DataStore();

        var primeira = Assert.IsType<AjusteBalanco>(loja.Movimentacoes.First());
        Assert.Equal("PRADOR, MELOXICAM 1 MG + DIPIRONA 50 MG - INJETÁVEL, FRASCO 100 ML", primeira.Produto.Nome);
        Assert.Equal(new DateTime(2026, 1, 20), primeira.Data);
        Assert.Equal((0, 6), (primeira.QuantidadeAnterior, primeira.QuantidadeApurada));
    }

    [Fact]
    public void HistoricoReal_CompraComAF_FoiInterpretada()
    {
        var loja = new DataStore();

        var compras = loja.Movimentacoes.OfType<EntradaEstoque>().Where(e => e.TipoOrigem == TipoOrigemEntrada.Compra).ToList();
        Assert.Equal(19, compras.Count);
        Assert.Equal(18, compras.Count(e => e.NumeroAF is not null));
        Assert.Contains(compras, e => e.NumeroAF == "002000/2025");
    }

    [Fact]
    public void CancelarSaidaImportada_DevolveAQuantidadeAoEstoque()
    {
        var loja = new DataStore();
        var movimentacoes = new MovimentacaoService(loja);
        var saida = loja.Movimentacoes.OfType<SaidaEstoque>().First(s => s.Quantidade > 0);
        var antes = saida.Produto.QuantidadeAtual;

        movimentacoes.Cancelar(saida.Id, "Teste de cancelamento");

        Assert.Equal(antes + saida.Quantidade, saida.Produto.QuantidadeAtual);
    }

    [Fact]
    public void Relatorio_UsaOHistoricoImportado()
    {
        var loja = new DataStore();
        var relatorio = new RelatorioService(loja);

        var itens = relatorio.GerarRelatorioConsumo(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        Assert.True(itens.Count > 50);
        Assert.Equal(loja.Movimentacoes.OfType<SaidaEstoque>().Sum(s => s.Quantidade), itens.Sum(i => i.TotalSaidas));
    }

    // Valores conferidos contra a planilha original, por conta independente: 737 linhas validas, 24 sem produto correspondente.
    private const int EsperadoImportadas = 713;
    private const int EsperadoAjustes = 218;
    private const int EsperadoEntradas = 100;
    private const int EsperadoSaidas = 395;
}
