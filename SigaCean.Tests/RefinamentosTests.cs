using SigaCean.Domain;
using SigaCean.Infrastructure;
using SigaCean.Services;

namespace SigaCean.Tests;

// Refinamentos do backend: o servico se protege sozinho (retirante, duplicidade, ids inexistentes) e a previsao FEFO
// mostrada na tela usa exatamente a mesma regra da saida.
public class RefinamentosTests
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);

    private readonly DataStore _dataStore = new();
    private readonly MovimentacaoService _movimentacoes;
    private readonly ProdutoService _produtos;

    public RefinamentosTests()
    {
        _dataStore.Produtos.Clear();
        _dataStore.Movimentacoes.Clear();
        _movimentacoes = new MovimentacaoService(_dataStore);
        _produtos = new ProdutoService(_dataStore);
    }

    private Produto NovoProdutoComEstoque()
    {
        var p = _produtos.Cadastrar("Produto Teste", CategoriaProduto.Medicamento, null, 5);
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(60), null);  // vence depois
        _movimentacoes.RegistrarEntrada(p, 4, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(10), null);   // vence antes
        _movimentacoes.RegistrarEntrada(p, 6, TipoOrigemEntrada.Doacao, null, null, null, null);               // sem validade
        return p;
    }

    // ---- Retirado por e obrigatorio no servico ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Saida_SemRetiradoPor_Falha_ENadaMuda(string? retiradoPor)
    {
        var p = NovoProdutoComEstoque();
        var historicoAntes = _dataStore.Movimentacoes.Count;

        Assert.Throws<InvalidOperationException>(() =>
            _movimentacoes.RegistrarSaida(p, 3, "Canil", null, retiradoPor!));

        Assert.Equal(20, p.QuantidadeAtual);
        Assert.Equal(historicoAntes, _dataStore.Movimentacoes.Count);
    }

    [Fact]
    public void Saida_GuardaRetiradoPorSemEspacosSobrando()
    {
        var p = NovoProdutoComEstoque();

        var saida = _movimentacoes.RegistrarSaida(p, 3, "Canil", null, "  Maria  ");

        Assert.Equal("Maria", saida.RetiradoPor);
    }

    // ---- Previsao FEFO ----

    [Fact]
    public void PrevisaoDeSaida_SegueFefo_ESemValidadeSaiPorUltimo()
    {
        var p = NovoProdutoComEstoque();

        var previsao = _movimentacoes.PrevisaoDeSaida(p, 7);

        Assert.Equal(2, previsao.Count);
        Assert.Equal(new LoteConsumido(Hoje.AddDays(10), 4), previsao[0]);
        Assert.Equal(new LoteConsumido(Hoje.AddDays(60), 3), previsao[1]);
    }

    [Fact]
    public void PrevisaoDeSaida_NaoAlteraNada()
    {
        var p = NovoProdutoComEstoque();
        var historicoAntes = _dataStore.Movimentacoes.Count;

        _movimentacoes.PrevisaoDeSaida(p, 15);

        Assert.Equal(20, p.QuantidadeAtual);
        Assert.Equal(3, p.Lotes.Count);
        Assert.Equal(historicoAntes, _dataStore.Movimentacoes.Count);
    }

    [Fact]
    public void PrevisaoDeSaida_ECoincideComAQueASaidaDepoisConsome()
    {
        var p = NovoProdutoComEstoque();

        var previsao = _movimentacoes.PrevisaoDeSaida(p, 16);
        var saida = _movimentacoes.RegistrarSaida(p, 16, "Canil", null, "Maria");

        Assert.Equal(previsao, saida.LotesConsumidos);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    public void PrevisaoDeSaida_ComQuantidadeInvalidaOuMaiorQueOEstoque_Falha(int quantidade)
    {
        var p = NovoProdutoComEstoque();

        Assert.Throws<InvalidOperationException>(() => _movimentacoes.PrevisaoDeSaida(p, quantidade));
    }

    [Fact]
    public void MensagemDeEstoqueInsuficiente_TemAcentuacao()
    {
        var p = NovoProdutoComEstoque();

        var erro = Assert.Throws<InvalidOperationException>(() => _movimentacoes.RegistrarSaida(p, 99, "Canil", null, "Maria"));

        Assert.Contains("disponível", erro.Message);
    }

    // ---- Proxima validade ----

    [Fact]
    public void ProximaValidade_E_AMenorDataDoQueAindaTemEstoque()
    {
        var p = NovoProdutoComEstoque();

        Assert.Equal(Hoje.AddDays(10), p.ProximaValidade);

        _movimentacoes.RegistrarSaida(p, 4, "Canil", null, "Maria"); // zera o saldo que vencia antes

        Assert.Equal(Hoje.AddDays(60), p.ProximaValidade);
    }

    [Fact]
    public void ProximaValidade_SemNenhumaDataInformada_ENula()
    {
        var p = _produtos.Cadastrar("Sem data", CategoriaProduto.Insumo, null, null);
        Assert.Null(p.ProximaValidade);

        _movimentacoes.RegistrarEntrada(p, 5, TipoOrigemEntrada.Compra, null, null, null, null);
        Assert.Null(p.ProximaValidade);
    }

    // ---- Ids inexistentes: mensagem clara, nunca erro tecnico ----

    [Fact]
    public void ObterPorId_Inexistente_DaMensagemClara()
    {
        var erro = Assert.Throws<InvalidOperationException>(() => _produtos.ObterPorId(Guid.NewGuid()));

        Assert.Equal("Produto não encontrado.", erro.Message);
    }

    [Fact]
    public void CorrigirECancelar_ComIdInexistente_DaMensagemClara()
    {
        var corrigir = Assert.Throws<InvalidOperationException>(() => _movimentacoes.Corrigir(Guid.NewGuid(), "erro"));
        var cancelar = Assert.Throws<InvalidOperationException>(() => _movimentacoes.Cancelar(Guid.NewGuid(), "erro"));

        Assert.Equal("Movimentação não encontrada.", corrigir.Message);
        Assert.Equal("Movimentação não encontrada.", cancelar.Message);
    }

    // ---- Duplicidade de produto ----

    [Fact]
    public void Cadastrar_ProdutoIdentico_Falha_ENaoCria()
    {
        _produtos.Cadastrar("Dipirona 500mg", CategoriaProduto.Medicamento, "Comprimido", 10);

        Assert.Throws<InvalidOperationException>(() =>
            _produtos.Cadastrar("  DIPIRONA 500MG ", CategoriaProduto.Medicamento, " comprimido ", null));

        Assert.Single(_dataStore.Produtos);
    }

    [Fact]
    public void Cadastrar_MesmoNomeComOutraApresentacao_Permite()
    {
        _produtos.Cadastrar("Dipirona 500mg", CategoriaProduto.Medicamento, "Comprimido", null);

        _produtos.Cadastrar("Dipirona 500mg", CategoriaProduto.Medicamento, "Injetável", null);
        _produtos.Cadastrar("Dipirona 500mg", CategoriaProduto.Medicamento, null, null);

        Assert.Equal(3, _dataStore.Produtos.Count);
    }

    [Fact]
    public void Cadastrar_NomeApenasParecido_Permite_ATelaQuemAvisa()
    {
        _produtos.Cadastrar("DIPIRONA - AMPOLA", CategoriaProduto.Medicamento, null, null);

        _produtos.Cadastrar("DIPIRONA, AMPOLA", CategoriaProduto.Medicamento, null, null);

        Assert.Equal(2, _dataStore.Produtos.Count);
    }

    [Fact]
    public void Alterar_ParaVirarIgualAOutroProduto_Falha_ENadaMuda()
    {
        _produtos.Cadastrar("Dipirona", CategoriaProduto.Medicamento, null, null);
        var outro = _produtos.Cadastrar("Ração", CategoriaProduto.Alimentar, null, 3);

        Assert.Throws<InvalidOperationException>(() =>
            _produtos.Alterar(outro.Id, "dipirona", CategoriaProduto.Medicamento, null, 9));

        Assert.Equal("Ração", outro.Nome);
        Assert.Equal(3, outro.EstoqueMinimo);
    }

    [Fact]
    public void Alterar_MantendoOProprioNome_Permite()
    {
        var p = _produtos.Cadastrar("Dipirona", CategoriaProduto.Medicamento, null, 2);

        _produtos.Alterar(p.Id, "Dipirona", CategoriaProduto.Medicamento, null, 8);

        Assert.Equal(8, p.EstoqueMinimo);
        Assert.False(_produtos.ExisteIdentico("Dipirona", null, p.Id));
        Assert.True(_produtos.ExisteIdentico("Dipirona", null));
    }
}
