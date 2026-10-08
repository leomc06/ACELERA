using SigaCean.Domain;
using SigaCean.Infrastructure;
using SigaCean.Services;

namespace SigaCean.Tests;

public class ReversaoDeMovimentacaoTests
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);

    private readonly DataStore _dataStore = new();
    private readonly MovimentacaoService _movimentacoes;
    private readonly ProdutoService _produtos;

    public ReversaoDeMovimentacaoTests()
    {
        _dataStore.Produtos.Clear(); // cenario proprio, sem os dados reais do CEAN
        _dataStore.Movimentacoes.Clear();
        _movimentacoes = new MovimentacaoService(_dataStore);
        _produtos = new ProdutoService(_dataStore);
    }

    private Produto NovoProduto() => _produtos.Cadastrar("Produto Teste", CategoriaProduto.Medicamento, null, 10);

    // ---- Entrada ----

    [Fact]
    public void CancelarEntrada_RetiraAQuantidadeDoEstoque()
    {
        var p = NovoProduto();
        var entrada = _movimentacoes.RegistrarEntrada(p, 20, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(60), null);

        _movimentacoes.Cancelar(entrada.Id, "Lançada por engano");

        Assert.Equal(0, p.QuantidadeAtual);
        Assert.Empty(p.Lotes);
        Assert.Equal(StatusMovimentacao.Cancelada, entrada.Status);
    }

    [Fact]
    public void CancelarEntrada_RetiraDoEstoqueComAMesmaValidade()
    {
        var p = NovoProduto();
        var validade = Hoje.AddDays(40);
        var entrada = _movimentacoes.RegistrarEntrada(p, 8, TipoOrigemEntrada.Doacao, null, null, validade, null);

        _movimentacoes.Cancelar(entrada.Id, "Duplicada");

        Assert.Equal(0, p.QuantidadeAtual);
    }

    [Fact]
    public void CancelarEntrada_QuandoEstoqueJaFoiUsado_Falha_ENadaMuda()
    {
        var p = NovoProduto();
        var entrada = _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, null, null);
        _movimentacoes.RegistrarSaida(p, 7, "Canil", null);

        Assert.Throws<InvalidOperationException>(() => _movimentacoes.Cancelar(entrada.Id, "Engano"));

        Assert.Equal(3, p.QuantidadeAtual);
        Assert.Equal(StatusMovimentacao.Ativa, entrada.Status);
        Assert.Null(entrada.MotivoCorrecaoOuCancelamento);
    }

    [Fact]
    public void CancelarEntrada_NaoMexeNoOutroLote()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(30), null);
        var segunda = _movimentacoes.RegistrarEntrada(p, 5, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(90), null);

        _movimentacoes.Cancelar(segunda.Id, "Engano");

        Assert.Equal(10, p.QuantidadeAtual);
        Assert.Equal(Hoje.AddDays(30), p.Lotes.Single().DataValidade);
    }

    // ---- Saída ----

    [Fact]
    public void CancelarSaida_DevolveAoMesmoLoteDeOrigem()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(10), null);
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(90), null);
        var saida = _movimentacoes.RegistrarSaida(p, 14, "Canil", null); // 10 de CEDO + 4 de TARDE

        _movimentacoes.Cancelar(saida.Id, "Destino errado");

        Assert.Equal(20, p.QuantidadeAtual);
        var cedo = p.Lotes.Single(l => l.DataValidade == Hoje.AddDays(10));
        Assert.Equal(10, cedo.Quantidade);
        Assert.Equal(Hoje.AddDays(10), cedo.DataValidade);
        Assert.Equal(10, p.Lotes.Single(l => l.DataValidade == Hoje.AddDays(90)).Quantidade);
    }

    [Fact]
    public void CancelarSaida_QueEsvaziouOLote_RecriaOLoteComValidade()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 5, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(15), null);
        var saida = _movimentacoes.RegistrarSaida(p, 5, "Canil", null);
        Assert.Empty(p.Lotes);

        _movimentacoes.Cancelar(saida.Id, "Engano");

        var lote = Assert.Single(p.Lotes);
        Assert.Equal(Hoje.AddDays(15), lote.DataValidade);
        Assert.Equal(5, lote.Quantidade);
    }

    [Fact]
    public void CorrigirSaida_TambemDevolveOEstoque()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, null, null);
        var saida = _movimentacoes.RegistrarSaida(p, 4, "Canil", null);

        _movimentacoes.Corrigir(saida.Id, "Quantidade errada");

        Assert.Equal(10, p.QuantidadeAtual);
        Assert.Equal(StatusMovimentacao.Corrigida, saida.Status);
    }

    // ---- Ajuste de balanço ----

    [Fact]
    public void CancelarAjusteParaCima_RetiraADiferenca()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, null, null);
        var ajuste = _movimentacoes.RegistrarAjusteBalanco(p, 15, null);

        _movimentacoes.Cancelar(ajuste.Id, "Contagem errada");

        Assert.Equal(10, p.QuantidadeAtual);
    }

    [Fact]
    public void CancelarAjusteParaBaixo_DevolveADiferencaAosLotesDeOrigem()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(20), null);
        var ajuste = _movimentacoes.RegistrarAjusteBalanco(p, 4, null);

        _movimentacoes.Cancelar(ajuste.Id, "Contagem errada");

        Assert.Equal(10, p.QuantidadeAtual);
        Assert.Equal(Hoje.AddDays(20), p.Lotes.Single().DataValidade);
    }

    [Fact]
    public void CancelarAjusteSemDiferenca_NaoAlteraEstoque()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, null, null);
        var ajuste = _movimentacoes.RegistrarAjusteBalanco(p, 10, null);

        _movimentacoes.Cancelar(ajuste.Id, "Sem efeito");

        Assert.Equal(10, p.QuantidadeAtual);
    }

    // ---- Só movimentação ativa ----

    [Fact]
    public void CancelarDuasVezes_Falha_ENaoDesfazDeNovo()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, null, null);
        var saida = _movimentacoes.RegistrarSaida(p, 4, "Canil", null);
        _movimentacoes.Cancelar(saida.Id, "Engano");

        Assert.Throws<InvalidOperationException>(() => _movimentacoes.Cancelar(saida.Id, "De novo"));
        Assert.Throws<InvalidOperationException>(() => _movimentacoes.Corrigir(saida.Id, "De novo"));

        Assert.Equal(10, p.QuantidadeAtual);
    }

    // ---- Relatório ignora o que foi desfeito ----

    [Fact]
    public void Relatorio_IgnoraMovimentacoesCanceladasECorrigidas()
    {
        var relatorio = new RelatorioService(_dataStore);
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 20, TipoOrigemEntrada.Compra, null, null, null, null);
        var saidaCancelada = _movimentacoes.RegistrarSaida(p, 5, "Canil", null);
        var saidaCorrigida = _movimentacoes.RegistrarSaida(p, 3, "Gatil", null);
        _movimentacoes.RegistrarSaida(p, 2, "Canil", null);
        _movimentacoes.Cancelar(saidaCancelada.Id, "Engano");
        _movimentacoes.Corrigir(saidaCorrigida.Id, "Quantidade errada");

        var item = relatorio.GerarRelatorioConsumo(DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1)).Single();

        Assert.Equal(20, item.TotalEntradas);
        Assert.Equal(2, item.TotalSaidas);
    }

    [Fact]
    public void Historico_ContinuaMostrandoMovimentacoesCanceladas()
    {
        var p = NovoProduto();
        var entrada = _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, null, null);
        _movimentacoes.Cancelar(entrada.Id, "Engano");

        var historico = _movimentacoes.ObterHistorico();

        Assert.Single(historico);
        Assert.Equal(StatusMovimentacao.Cancelada, historico[0].Status);
    }
}
