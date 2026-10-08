using SigaCean.Domain;
using SigaCean.Infrastructure;
using SigaCean.Services;

namespace SigaCean.Tests;

// Melhorias pedidas a partir das respostas do CEAN: retirante, documento, filtros do estoque e periodos de relatorio.
public class MelhoriasDeUsoTests
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);

    private readonly DataStore _dataStore = new();
    private readonly MovimentacaoService _movimentacoes;
    private readonly ProdutoService _produtos;
    private readonly EstoqueService _estoque;
    private readonly RelatorioService _relatorio;

    public MelhoriasDeUsoTests()
    {
        _dataStore.Produtos.Clear(); // cenario proprio, sem os dados reais do CEAN
        _dataStore.Movimentacoes.Clear();
        _movimentacoes = new MovimentacaoService(_dataStore);
        _produtos = new ProdutoService(_dataStore);
        _estoque = new EstoqueService(_dataStore);
        _relatorio = new RelatorioService(_dataStore);
    }

    private Produto Novo(string nome, CategoriaProduto categoria = CategoriaProduto.Medicamento, int? minimo = null) =>
        _produtos.Cadastrar(nome, categoria, null, minimo);

    // ---- Quem retirou / documento da entrada ----

    [Fact]
    public void Saida_GuardaQuemRetirou()
    {
        var p = Novo("Dipirona");
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, null, null);

        var saida = _movimentacoes.RegistrarSaida(p, 3, "Canil", null, "Maria");

        Assert.Equal("Maria", saida.RetiradoPor);
        Assert.Equal(7, p.QuantidadeAtual);
    }

    [Fact]
    public void Entrada_GuardaODocumento_NotaFiscalOuPedido()
    {
        var p = Novo("Dipirona");

        var entrada = _movimentacoes.RegistrarEntrada(p, 5, TipoOrigemEntrada.Concessao, null, "Secretaria de Saúde", null, null, "NF 1234");

        Assert.Equal("NF 1234", entrada.Documento);
    }

    // ---- Filtros do estoque ----

    [Fact]
    public void Filtrar_PorNomeECategoria()
    {
        Novo("Dipirona 500 mg", CategoriaProduto.Medicamento);
        Novo("Dipirona gotas", CategoriaProduto.Medicamento);
        Novo("Ração porte médio", CategoriaProduto.Alimentar);

        Assert.Equal(2, _estoque.Filtrar("dipirona", null, SituacaoEstoque.Todos).Count);
        Assert.Single(_estoque.Filtrar(null, CategoriaProduto.Alimentar, SituacaoEstoque.Todos));
        Assert.Empty(_estoque.Filtrar("dipirona", CategoriaProduto.Alimentar, SituacaoEstoque.Todos));
    }

    [Fact]
    public void Filtrar_PorSituacao_CriticoVencidoEValidadeProxima()
    {
        var critico = Novo("Critico", minimo: 100);
        _movimentacoes.RegistrarEntrada(critico, 10, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(400), null);

        var vencido = Novo("Vencido");
        _movimentacoes.RegistrarEntrada(vencido, 10, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(-1), null);

        var proximo = Novo("Proximo");
        _movimentacoes.RegistrarEntrada(proximo, 10, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(10), null);

        Assert.Equal(["Critico"], _estoque.Filtrar(null, null, SituacaoEstoque.Critico).Select(p => p.Nome));
        Assert.Equal(["Vencido"], _estoque.Filtrar(null, null, SituacaoEstoque.Vencido).Select(p => p.Nome));
        Assert.Equal(["Proximo"], _estoque.Filtrar(null, null, SituacaoEstoque.ValidadeProxima).Select(p => p.Nome));
        Assert.Equal(1, _estoque.Contar(SituacaoEstoque.Vencido));
        Assert.Equal(3, _estoque.Contar(SituacaoEstoque.Todos));
    }

    // ---- Periodos do relatorio ----

    [Theory]
    [InlineData(TipoPeriodo.Mensal, "2026-02-17", "2026-02-01", "2026-02-28")]
    [InlineData(TipoPeriodo.Trimestral, "2026-05-10", "2026-04-01", "2026-06-30")]
    [InlineData(TipoPeriodo.Trimestral, "2026-12-31", "2026-10-01", "2026-12-31")]
    [InlineData(TipoPeriodo.Anual, "2026-08-08", "2026-01-01", "2026-12-31")]
    public void Periodo_ContendoADataDeReferencia(TipoPeriodo tipo, string referencia, string inicio, string fim)
    {
        var (i, f) = PeriodoRelatorio.Contendo(tipo, DateTime.Parse(referencia));

        Assert.Equal((DateTime.Parse(inicio), DateTime.Parse(fim)), (i, f));
    }

    [Fact]
    public void Periodo_AnteriorEProximo_AtravessamOAno()
    {
        var (inicio, fim) = PeriodoRelatorio.Deslocar(TipoPeriodo.Mensal, new DateTime(2026, 1, 1), -1);
        Assert.Equal((new DateTime(2025, 12, 1), new DateTime(2025, 12, 31)), (inicio, fim));

        (inicio, fim) = PeriodoRelatorio.Deslocar(TipoPeriodo.Trimestral, new DateTime(2026, 10, 1), 1);
        Assert.Equal((new DateTime(2027, 1, 1), new DateTime(2027, 3, 31)), (inicio, fim));

        (inicio, fim) = PeriodoRelatorio.Deslocar(TipoPeriodo.Anual, new DateTime(2026, 1, 1), -1);
        Assert.Equal((new DateTime(2025, 1, 1), new DateTime(2025, 12, 31)), (inicio, fim));
    }

    [Fact]
    public void Periodo_Descricao()
    {
        Assert.Equal("outubro de 2026", PeriodoRelatorio.Descrever(TipoPeriodo.Mensal, new DateTime(2026, 10, 1), new DateTime(2026, 10, 31)));
        Assert.Equal("4º trimestre de 2026 (outubro a dezembro)", PeriodoRelatorio.Descrever(TipoPeriodo.Trimestral, new DateTime(2026, 10, 1), new DateTime(2026, 12, 31)));
        Assert.Equal("2026", PeriodoRelatorio.Descrever(TipoPeriodo.Anual, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)));
    }

    // ---- Relatorio de consumo ----

    private void SaidaEm(Produto p, int quantidade, DateTime data)
    {
        // A data da movimentacao e "agora"; para testar periodos, ajusta-se a movimentacao registrada.
        var saida = _movimentacoes.RegistrarSaida(p, quantidade, "Canil", null, "Maria");
        _dataStore.Movimentacoes.Remove(saida);
        _dataStore.Movimentacoes.Add(new SaidaEstoque { Produto = p, Quantidade = quantidade, Destino = "Canil", Data = data });
    }

    [Fact]
    public void Relatorio_FiltraPorCategoria_EOrdenaPorMaiorConsumo()
    {
        var a = Novo("A remédio", CategoriaProduto.Medicamento);
        var b = Novo("B remédio", CategoriaProduto.Medicamento);
        var c = Novo("C ração", CategoriaProduto.Alimentar);
        foreach (var p in new[] { a, b, c })
            _movimentacoes.RegistrarEntrada(p, 100, TipoOrigemEntrada.Compra, null, null, null, null);
        SaidaEm(a, 5, DateTime.Today);
        SaidaEm(b, 30, DateTime.Today);
        SaidaEm(c, 50, DateTime.Today);
        var (inicio, fim) = PeriodoRelatorio.Contendo(TipoPeriodo.Mensal, DateTime.Today);

        var soMedicamentos = _relatorio.GerarRelatorioConsumo(inicio, fim.AddDays(1), CategoriaProduto.Medicamento, OrdemRelatorio.MaiorConsumo);

        Assert.Equal(["B remédio", "A remédio"], soMedicamentos.Select(i => i.Produto.Nome));
    }

    [Fact]
    public void Relatorio_TrazEstoqueAtual_EMediaMensal()
    {
        var p = Novo("Produto");
        _movimentacoes.RegistrarEntrada(p, 100, TipoOrigemEntrada.Compra, null, null, null, null);
        // 100 saidas num periodo de 4 meses (jan a abr) => media de ~25 por mes
        SaidaEm(p, 100, new DateTime(2026, 2, 10));
        var inicio = new DateTime(2026, 1, 1);
        var fim = new DateTime(2026, 4, 30);

        var item = _relatorio.GerarRelatorioConsumo(inicio, fim).Single();

        Assert.Equal(p.QuantidadeAtual, item.EstoqueAtual);
        Assert.Equal(100, item.TotalSaidas);
        Assert.Equal(Math.Round(100 / PeriodoRelatorio.MesesNoPeriodo(inicio, fim), 1), item.MediaMensalSaidas);
        Assert.InRange(item.MediaMensalSaidas, 24m, 26m);
    }
}
