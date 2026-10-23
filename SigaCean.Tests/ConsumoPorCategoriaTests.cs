using SigaCean.Domain;
using SigaCean.Infrastructure;
using SigaCean.Presentation;
using SigaCean.Services;

namespace SigaCean.Tests;

// "Consumo por categoria": alimenta os graficos e o relatorio por categoria. "Uso" = saidas ATIVAS no periodo.
public class ConsumoPorCategoriaTests
{
    private static readonly DateTime Inicio = DateTime.Today.AddDays(-30);
    private static readonly DateTime Fim = DateTime.Today.AddDays(1).AddTicks(-1);

    private readonly DataStore _dataStore = new();
    private readonly ProdutoService _produtos;
    private readonly MovimentacaoService _movimentacoes;
    private readonly RelatorioService _relatorio;

    public ConsumoPorCategoriaTests()
    {
        _dataStore.Produtos.Clear(); // cenario proprio, sem os dados reais do CEAN
        _dataStore.Movimentacoes.Clear();
        _produtos = new ProdutoService(_dataStore);
        _movimentacoes = new MovimentacaoService(_dataStore);
        _relatorio = new RelatorioService(_dataStore);
    }

    private Produto ComEstoque(string nome, int quantidade, CategoriaProduto categoria = CategoriaProduto.Medicamento)
    {
        var produto = _produtos.Cadastrar(nome, categoria, null, null);
        _movimentacoes.RegistrarEntrada(produto, quantidade, TipoOrigemEntrada.Compra, null, null, null, null);
        return produto;
    }

    private void Saida(Produto produto, int quantidade) =>
        _movimentacoes.RegistrarSaida(produto, quantidade, "Canil", null, "Maria");

    private ConsumoPorCategoria Linha(CategoriaProduto categoria) =>
        _relatorio.GerarConsumoPorCategoria(Inicio, Fim).Single(l => l.Categoria == categoria);

    [Fact]
    public void SemNadaCadastrado_DevolveUmaLinhaPorCategoria_TodaZerada()
    {
        var linhas = _relatorio.GerarConsumoPorCategoria(Inicio, Fim);

        Assert.Equal(Enum.GetValues<CategoriaProduto>().Length, linhas.Count);
        Assert.All(linhas, l =>
        {
            Assert.Equal(0, l.QuantidadeDeProdutos);
            Assert.Equal(0, l.EstoqueAtual);
            Assert.Equal(0, l.TotalSaidas);
            Assert.Null(l.MaisUsado);
            Assert.Null(l.MenosUsado);
        });
    }

    [Fact]
    public void EstoqueEQuantidadeDeProdutos_SomamPorCategoria()
    {
        ComEstoque("Dipirona", 10);
        ComEstoque("Vacina", 5);
        ComEstoque("Racao", 20, CategoriaProduto.Alimentar);

        var medicamento = Linha(CategoriaProduto.Medicamento);

        Assert.Equal(2, medicamento.QuantidadeDeProdutos);
        Assert.Equal(15, medicamento.EstoqueAtual);
        Assert.Equal(20, Linha(CategoriaProduto.Alimentar).EstoqueAtual);
        Assert.Equal(0, Linha(CategoriaProduto.Higiene).QuantidadeDeProdutos);
    }

    [Fact]
    public void MaisEMenosUsado_SaemDasSaidasDoPeriodo()
    {
        var dipirona = ComEstoque("Dipirona", 50);
        var vacina = ComEstoque("Vacina", 50);
        var soro = ComEstoque("Soro", 50);
        Saida(dipirona, 10);
        Saida(dipirona, 5);
        Saida(vacina, 2);
        Saida(soro, 7);

        var linha = Linha(CategoriaProduto.Medicamento);

        Assert.Equal(24, linha.TotalSaidas);
        Assert.Equal("Dipirona", linha.MaisUsado!.Produto.Nome);
        Assert.Equal(15, linha.MaisUsado.TotalSaidas);
        Assert.Equal("Vacina", linha.MenosUsado!.Produto.Nome);
        Assert.Equal(2, linha.MenosUsado.TotalSaidas);
        Assert.Equal(new[] { "Dipirona", "Soro", "Vacina" }, linha.ProdutosUsados.Select(c => c.Produto.Nome));
    }

    [Fact]
    public void ProdutoSemSaida_NaoEMenosUsado_EEContadoApartir()
    {
        var usado = ComEstoque("Usado", 20);
        var parado = ComEstoque("Parado", 20);
        Saida(usado, 3);

        var linha = Linha(CategoriaProduto.Medicamento);

        Assert.Equal("Usado", linha.MaisUsado!.Produto.Nome);
        Assert.Null(linha.MenosUsado); // so um produto teve saida: nao ha com quem comparar
        Assert.Equal(1, linha.ProdutosSemSaida);
        Assert.DoesNotContain(linha.ProdutosUsados, c => c.Produto == parado);
    }

    [Fact]
    public void EmpateDeSaidas_DesempataPorNome()
    {
        var b = ComEstoque("Beta", 20);
        var a = ComEstoque("Alfa", 20);
        Saida(b, 4);
        Saida(a, 4);

        var linha = Linha(CategoriaProduto.Medicamento);

        Assert.Equal("Alfa", linha.MaisUsado!.Produto.Nome);
        Assert.Equal("Beta", linha.MenosUsado!.Produto.Nome);
    }

    [Fact]
    public void SoContamSaidasAtivas_CanceladasECorrigidasFicamDeFora()
    {
        var produto = ComEstoque("Dipirona", 50);
        var cancelada = _movimentacoes.RegistrarSaida(produto, 10, "Canil", null, "Maria");
        var corrigida = _movimentacoes.RegistrarSaida(produto, 8, "Canil", null, "Maria");
        Saida(produto, 3);
        _movimentacoes.Cancelar(cancelada.Id, "Lançada errada");
        _movimentacoes.Corrigir(corrigida.Id, "Quantidade errada");

        Assert.Equal(3, Linha(CategoriaProduto.Medicamento).TotalSaidas);
    }

    [Fact]
    public void EntradasEAjustes_NaoContamComoUso()
    {
        var produto = ComEstoque("Dipirona", 10);
        _movimentacoes.RegistrarEntrada(produto, 30, TipoOrigemEntrada.Compra, null, null, null, null);
        _movimentacoes.RegistrarAjusteBalanco(produto, 5, null);

        var linha = Linha(CategoriaProduto.Medicamento);

        Assert.Equal(0, linha.TotalSaidas);
        Assert.Null(linha.MaisUsado);
        Assert.Equal(1, linha.ProdutosSemSaida);
    }

    [Fact]
    public void SaidaForaDoPeriodo_NaoConta()
    {
        var produto = ComEstoque("Dipirona", 10);
        Saida(produto, 4);

        var linhas = _relatorio.GerarConsumoPorCategoria(DateTime.Today.AddYears(-3), DateTime.Today.AddYears(-2));

        Assert.All(linhas, l => Assert.Equal(0, l.TotalSaidas));
    }

    [Fact]
    public void ProdutoInativo_NaoEntraNaContagem()
    {
        var ativo = ComEstoque("Ativo", 10);
        var inativo = ComEstoque("Inativo", 10);
        Saida(inativo, 4);
        inativo.Ativo = false;

        var linha = Linha(CategoriaProduto.Medicamento);

        Assert.Equal(1, linha.QuantidadeDeProdutos);
        Assert.Equal(ativo.QuantidadeAtual, linha.EstoqueAtual);
        Assert.Equal(0, linha.TotalSaidas);
    }

    [Fact]
    public void ComOsDadosReaisDoCean_TodasAsCategoriasVem_ESomaBateComORelatorioPorProduto()
    {
        var loja = new DataStore(); // dados reais + historico importado
        var relatorio = new RelatorioService(loja);
        var inicio = new DateTime(2026, 1, 1);
        var fim = new DateTime(2026, 12, 31, 23, 59, 59);

        var porCategoria = relatorio.GerarConsumoPorCategoria(inicio, fim);
        var porProduto = relatorio.GerarRelatorioConsumo(inicio, fim);

        Assert.Equal(Enum.GetValues<CategoriaProduto>().Length, porCategoria.Count);
        Assert.Equal(porProduto.Sum(i => i.TotalSaidas), porCategoria.Sum(c => c.TotalSaidas));
        Assert.Equal(loja.Produtos.Where(p => p.Ativo).Sum(p => p.QuantidadeAtual), porCategoria.Sum(c => c.EstoqueAtual));
    }

    // ---- Textos ----

    [Fact]
    public void Textos_DescrevemMaisMenosUsadoESemSaida()
    {
        var dipirona = ComEstoque("Dipirona", 2000);
        var vacina = ComEstoque("Vacina", 50);
        ComEstoque("Parado", 5);
        Saida(dipirona, 1200);
        Saida(vacina, 2);
        var linha = Linha(CategoriaProduto.Medicamento);

        Assert.Equal("Mais usado: Dipirona (1.200 un.)", TextosDeConsumo.MaisUsado(linha));
        Assert.Equal("Menos usado: Vacina (2 un.)", TextosDeConsumo.MenosUsado(linha));
        Assert.Equal("1 produto(s) sem nenhuma saída no período.", TextosDeConsumo.SemSaida(linha));
        Assert.Contains("3 produto(s)", TextosDeConsumo.Resumo(linha));
    }

    [Fact]
    public void Textos_CategoriaSemProdutoOuSemUso()
    {
        var vazia = Linha(CategoriaProduto.Microchip);

        Assert.Equal("Nenhuma saída neste período.", TextosDeConsumo.MaisUsado(vazia));
        Assert.Equal(string.Empty, TextosDeConsumo.MenosUsado(vazia));
        Assert.Equal(string.Empty, TextosDeConsumo.SemSaida(vazia));
        Assert.Equal("Nenhum produto cadastrado nesta categoria.", TextosDeConsumo.Resumo(vazia));
    }

    [Theory]
    [InlineData(25, 100, "25%")]
    [InlineData(1, 3, "33%")]
    [InlineData(0, 50, "0%")]
    [InlineData(5, 0, "—")]
    [InlineData(1, 4621, "<1%")]
    public void Percentual_ArredondaEProtegeDivisaoPorZero(double valor, double total, string esperado)
    {
        Assert.Equal(esperado, TextosDeConsumo.Percentual(valor, total));
    }

    [Fact]
    public void Encurtar_CortaNomesLongosComReticencias()
    {
        Assert.Equal("Dipirona", TextosDeConsumo.Encurtar("Dipirona", 20));
        Assert.Equal("Dipirona 500 mg…", TextosDeConsumo.Encurtar("Dipirona 500 mg injetável ampola", 16));
    }
}
