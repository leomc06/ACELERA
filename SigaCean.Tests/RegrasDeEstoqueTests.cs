using SigaCean.Domain;
using SigaCean.Infrastructure;
using SigaCean.Services;

namespace SigaCean.Tests;

public class RegrasDeEstoqueTests
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);

    private readonly DataStore _dataStore = new();
    private readonly MovimentacaoService _movimentacoes;
    private readonly ProdutoService _produtos;
    private readonly EstoqueService _estoque;

    public RegrasDeEstoqueTests()
    {
        _dataStore.Produtos.Clear(); // os testes montam o proprio cenario, sem os dados reais do CEAN
        _dataStore.Movimentacoes.Clear();
        _movimentacoes = new MovimentacaoService(_dataStore);
        _produtos = new ProdutoService(_dataStore);
        _estoque = new EstoqueService(_dataStore);
    }

    private Produto NovoProduto(string nome = "Produto Teste", int? minimo = 10) =>
        _produtos.Cadastrar(nome, CategoriaProduto.Medicamento, null, minimo);

    // ---- RD0002 / RD0003: entrada aumenta, saída reduz ----

    [Fact]
    public void Entrada_AumentaEstoque_ERegistraMovimentacao()
    {
        var p = NovoProduto();

        _movimentacoes.RegistrarEntrada(p, 20, TipoOrigemEntrada.Doacao, null, "Doador", null, null);

        Assert.Equal(20, p.QuantidadeAtual);
        Assert.Single(_dataStore.Movimentacoes.OfType<EntradaEstoque>(), m => m.Produto == p);
    }

    [Fact]
    public void Entrada_ComAMesmaValidade_SomaNoMesmoSaldo()
    {
        var p = NovoProduto();

        _movimentacoes.RegistrarEntrada(p, 5, TipoOrigemEntrada.Compra, "AF1", null, null, null);
        _movimentacoes.RegistrarEntrada(p, 7, TipoOrigemEntrada.Compra, "AF1", null, null, null);

        Assert.Single(p.Lotes);
        Assert.Equal(12, p.QuantidadeAtual);
    }

    [Fact]
    public void Saida_ReduzEstoque_ERegistraDestino()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 20, TipoOrigemEntrada.Compra, null, null, null, null);

        var saida = _movimentacoes.RegistrarSaida(p, 8, "Canil", null, "Teste");

        Assert.Equal(12, p.QuantidadeAtual);
        Assert.Equal("Canil", saida.Destino);
    }

    [Fact]
    public void Saida_MaiorQueDisponivel_Falha_ENaoAlteraEstoqueNemHistorico()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 5, TipoOrigemEntrada.Compra, null, null, null, null);
        var movimentacoesAntes = _dataStore.Movimentacoes.Count;

        Assert.Throws<InvalidOperationException>(() => _movimentacoes.RegistrarSaida(p, 6, "Canil", null, "Teste"));

        Assert.Equal(5, p.QuantidadeAtual);
        Assert.Equal(movimentacoesAntes, _dataStore.Movimentacoes.Count);
    }

    [Fact]
    public void Saida_ExatamenteTodoEstoque_ZeraSemErro()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 5, TipoOrigemEntrada.Compra, null, null, null, null);

        _movimentacoes.RegistrarSaida(p, 5, "Canil", null, "Teste");

        Assert.Equal(0, p.QuantidadeAtual);
    }

    // ---- FEFO ----

    [Fact]
    public void Saida_ConsomePrimeiroOLoteQueVenceMaisCedo()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(90), null);
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(10), null);

        _movimentacoes.RegistrarSaida(p, 6, "Canil", null, "Teste");

        Assert.Equal(4, p.Lotes.Single(l => l.DataValidade == Hoje.AddDays(10)).Quantidade);
        Assert.Equal(10, p.Lotes.Single(l => l.DataValidade == Hoje.AddDays(90)).Quantidade);
    }

    [Fact]
    public void Saida_AtravessaLotes_QuandoPrimeiroNaoBasta()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 4, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(10), null);
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(90), null);

        _movimentacoes.RegistrarSaida(p, 7, "Canil", null, "Teste");

        Assert.DoesNotContain(p.Lotes, l => l.DataValidade == Hoje.AddDays(10));
        Assert.Equal(7, p.Lotes.Single(l => l.DataValidade == Hoje.AddDays(90)).Quantidade);
    }

    [Fact]
    public void Saida_LoteSemValidade_EConsumidoPorUltimo()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, null, null);
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(30), null);

        _movimentacoes.RegistrarSaida(p, 10, "Canil", null, "Teste");

        Assert.DoesNotContain(p.Lotes, l => l.DataValidade == Hoje.AddDays(30));
        Assert.Equal(10, p.Lotes.Single(l => l.DataValidade == null).Quantidade);
    }

    // ---- Ajuste de balanço (RD0023) ----

    [Fact]
    public void Ajuste_ParaMais_AcrescentaDiferenca()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, null, null);

        var ajuste = _movimentacoes.RegistrarAjusteBalanco(p, 15, null);

        Assert.Equal(15, p.QuantidadeAtual);
        Assert.Equal(10, ajuste.QuantidadeAnterior);
        Assert.Equal(15, ajuste.QuantidadeApurada);
        Assert.Equal(5, ajuste.Quantidade);
    }

    [Fact]
    public void Ajuste_ParaMenos_ReduzDiferenca()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, null, null);

        var ajuste = _movimentacoes.RegistrarAjusteBalanco(p, 3, null);

        Assert.Equal(3, p.QuantidadeAtual);
        Assert.Equal(-7, ajuste.Quantidade);
    }

    [Fact]
    public void Ajuste_ParaMesmoValor_NaoAlteraEstoque_MasRegistraMovimentacao()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, null, null);

        var ajuste = _movimentacoes.RegistrarAjusteBalanco(p, 10, null);

        Assert.Equal(10, p.QuantidadeAtual);
        Assert.Equal(0, ajuste.Quantidade);
    }

    // ---- Correção / cancelamento (UC06) ----

    [Fact]
    public void Corrigir_MarcaStatusEMotivo()
    {
        var p = NovoProduto();
        var entrada = _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, null, null);

        _movimentacoes.Corrigir(entrada.Id, "Quantidade digitada errada");

        Assert.Equal(StatusMovimentacao.Corrigida, entrada.Status);
        Assert.Equal("Quantidade digitada errada", entrada.MotivoCorrecaoOuCancelamento);
    }

    [Fact]
    public void Cancelar_MarcaStatusEMotivo()
    {
        var p = NovoProduto();
        var entrada = _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, null, null);

        _movimentacoes.Cancelar(entrada.Id, "Lançamento duplicado");

        Assert.Equal(StatusMovimentacao.Cancelada, entrada.Status);
        Assert.Equal("Lançamento duplicado", entrada.MotivoCorrecaoOuCancelamento);
    }

    [Fact]
    public void MovimentacaoNova_ComecaAtiva()
    {
        var p = NovoProduto();
        var entrada = _movimentacoes.RegistrarEntrada(p, 1, TipoOrigemEntrada.Compra, null, null, null, null);

        Assert.Equal(StatusMovimentacao.Ativa, entrada.Status);
    }

    // ---- Saúde do estoque: cor e percentual ----

    [Theory]
    [InlineData(11, CorSaudeEstoque.Verde)]
    [InlineData(10, CorSaudeEstoque.Vermelho)] // atingiu o mínimo
    [InlineData(5, CorSaudeEstoque.Vermelho)]
    [InlineData(0, CorSaudeEstoque.Vermelho)]
    public void Saude_VermelhaQuandoAtingeOuFicaAbaixoDoMinimo(int quantidade, CorSaudeEstoque esperada)
    {
        var p = NovoProduto(minimo: 10);
        if (quantidade > 0) p.AdicionarQuantidade(quantidade, null);

        Assert.Equal(esperada, p.CorSaude);
    }

    [Fact]
    public void Saude_SemEstoqueMinimo_EVerde_ESemPercentual()
    {
        var p = NovoProduto(minimo: null);
        p.AdicionarQuantidade(1, null);

        Assert.Equal(CorSaudeEstoque.Verde, p.CorSaude);
        Assert.Null(p.PercentualSaude);
        Assert.False(p.EstaAbaixoDoMinimo);
        Assert.False(p.EstaCritico);
    }

    [Fact]
    public void Saude_PercentualEmRelacaoAoMinimo()
    {
        var p = NovoProduto(minimo: 50);
        p.AdicionarQuantidade(63, null);

        Assert.Equal(126, p.PercentualSaude);
    }

    [Theory]
    [InlineData(5, true)]  // exatamente 50% do mínimo
    [InlineData(6, false)]
    [InlineData(0, true)]
    public void Critico_QuandoEstaMuitoAbaixoDoMinimo(int quantidade, bool critico)
    {
        var p = NovoProduto(minimo: 10);
        if (quantidade > 0) p.AdicionarQuantidade(quantidade, null);

        Assert.Equal(critico, p.EstaCritico);
    }

    // ---- Alertas de validade (janela de 30 dias) ----

    [Fact]
    public void Validade_ProximaDoVencimento_DentroDaJanela()
    {
        var p = NovoProduto();
        p.AdicionarQuantidade(5, Hoje.AddDays(30));

        Assert.Contains(p, _estoque.ObterProximosDoVencimento(Hoje));
        Assert.DoesNotContain(p, _estoque.ObterVencidos(Hoje));
    }

    [Fact]
    public void Validade_ForaDaJanela_NaoAlerta()
    {
        var p = NovoProduto();
        p.AdicionarQuantidade(5, Hoje.AddDays(31));

        Assert.DoesNotContain(p, _estoque.ObterProximosDoVencimento(Hoje));
    }

    [Fact]
    public void Validade_Vencido_EClassificadoComoVencido_ENaoComoProximo()
    {
        var p = NovoProduto();
        p.AdicionarQuantidade(5, Hoje.AddDays(-1));

        Assert.Contains(p, _estoque.ObterVencidos(Hoje));
        Assert.DoesNotContain(p, _estoque.ObterProximosDoVencimento(Hoje));
    }

    [Fact]
    public void Validade_VenceHoje_AindaNaoEstaVencido()
    {
        var lote = new LoteEstoque { DataValidade = Hoje, Quantidade = 1 };

        Assert.False(lote.EstaVencido(Hoje));
        Assert.True(lote.EstaProximoDoVencimento(Hoje, 30));
    }

    [Fact]
    public void Validade_LoteSemData_NuncaAlerta()
    {
        var lote = new LoteEstoque { DataValidade = null, Quantidade = 1 };

        Assert.False(lote.EstaVencido(Hoje));
        Assert.False(lote.EstaProximoDoVencimento(Hoje, 30));
    }

    // ---- Duplicidade de cadastro ----

    [Theory]
    [InlineData("SULFATO DE MORFINA 10 MG - INJETÁVEL, AMPOLA", "SULFATO DE MORFINA 10 MG - INJETÁVEL - AMPOLA")]
    [InlineData("Dipirona 500mg", "dipirona  500 mg")]
    [InlineData("Ração Seca", "RAÇÃO SECA!")]
    public void Duplicidade_PontuacaoEEspacamentoNaoCriamProdutoNovo(string existente, string digitado)
    {
        _produtos.Cadastrar(existente, CategoriaProduto.Medicamento, null, null);

        Assert.NotNull(_produtos.ObterPorNomeSemelhante(digitado));
    }

    [Fact]
    public void Duplicidade_NomesDiferentes_NaoSaoParecidos()
    {
        _produtos.Cadastrar("Dipirona 500mg", CategoriaProduto.Medicamento, null, null);

        Assert.Null(_produtos.ObterPorNomeSemelhante("Dipirona 1g"));
    }

    [Fact]
    public void Duplicidade_NaEdicao_OProprioProdutoNaoContaComoDuplicado()
    {
        var p = _produtos.Cadastrar("Dipirona 500mg", CategoriaProduto.Medicamento, null, null);

        Assert.Null(_produtos.ObterPorNomeSemelhante("Dipirona 500mg", ignorarId: p.Id));
    }

    [Fact]
    public void Duplicidade_ProdutoInativo_NaoConta()
    {
        var p = _produtos.Cadastrar("Dipirona 500mg", CategoriaProduto.Medicamento, null, null);
        p.Ativo = false;

        Assert.Null(_produtos.ObterPorNomeSemelhante("Dipirona 500mg"));
    }

    // ---- Pesquisa (RF0004) ----

    [Fact]
    public void Pesquisa_PorNomeECategoria()
    {
        _produtos.Cadastrar("Vermífugo Canino", CategoriaProduto.Medicamento, null, null);
        _produtos.Cadastrar("Vermífugo Felino", CategoriaProduto.Medicamento, null, null);
        _produtos.Cadastrar("Ração Vermelha", CategoriaProduto.Alimentar, null, null);

        Assert.Equal(3, _produtos.Pesquisar("verm", null).Count);
        Assert.Equal(2, _produtos.Pesquisar("verm", CategoriaProduto.Medicamento).Count);
        Assert.Empty(_produtos.Pesquisar("inexistente", null));
    }

    // ---- Histórico (RF0011) ----

    [Fact]
    public void Historico_MaisRecentePrimeiro_EFiltraPorProduto()
    {
        var a = NovoProduto("A");
        var b = NovoProduto("B");
        _movimentacoes.RegistrarEntrada(a, 5, TipoOrigemEntrada.Compra, null, null, null, null);
        _movimentacoes.RegistrarEntrada(b, 5, TipoOrigemEntrada.Compra, null, null, null, null);
        _movimentacoes.RegistrarSaida(a, 1, "Canil", null, "Teste");

        var todos = _movimentacoes.ObterHistorico();
        var soA = _movimentacoes.ObterHistorico(produto: a);

        Assert.Equal(3, todos.Count);
        Assert.True(todos.SequenceEqual(todos.OrderByDescending(m => m.Data)));
        Assert.Equal(2, soA.Count);
        Assert.All(soA, m => Assert.Equal(a.Id, m.Produto.Id));
    }

    [Fact]
    public void Historico_FiltraPorPeriodo()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 5, TipoOrigemEntrada.Compra, null, null, null, null);

        Assert.Single(_movimentacoes.ObterHistorico(dataInicio: DateTime.Now.AddMinutes(-1), dataFim: DateTime.Now.AddMinutes(1)));
        Assert.Empty(_movimentacoes.ObterHistorico(dataInicio: DateTime.Now.AddDays(1)));
        Assert.Empty(_movimentacoes.ObterHistorico(dataFim: DateTime.Now.AddDays(-1)));
    }

    // ---- Relatório de consumo (UC08) ----

    [Fact]
    public void Relatorio_AgregaEntradasSaidasEAjustesPorProduto()
    {
        var relatorio = new RelatorioService(_dataStore);
        var a = NovoProduto("A");
        var b = NovoProduto("B");
        _movimentacoes.RegistrarEntrada(a, 20, TipoOrigemEntrada.Compra, null, null, null, null);
        _movimentacoes.RegistrarSaida(a, 5, "Canil", null, "Teste");
        _movimentacoes.RegistrarSaida(a, 3, "Gatil", null, "Teste");
        _movimentacoes.RegistrarAjusteBalanco(a, 10, null); // 12 -> 10 = -2
        _movimentacoes.RegistrarEntrada(b, 4, TipoOrigemEntrada.Doacao, null, null, null, null);

        var itens = relatorio.GerarRelatorioConsumo(DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1));

        var itemA = itens.Single(i => i.Produto == a);
        Assert.Equal(20, itemA.TotalEntradas);
        Assert.Equal(8, itemA.TotalSaidas);
        Assert.Equal(-2, itemA.TotalAjustes);
        Assert.Equal(4, itens.Single(i => i.Produto == b).TotalEntradas);
    }

    [Fact]
    public void Relatorio_ForaDoPeriodo_NaoAparece()
    {
        var relatorio = new RelatorioService(_dataStore);
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 20, TipoOrigemEntrada.Compra, null, null, null, null);

        Assert.Empty(relatorio.GerarRelatorioConsumo(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(-10)));
    }

    // ---- Login (dois perfis com senha) ----

    [Fact]
    public void Login_Administrativo_E_Veterinario_ComSenhaCorreta()
    {
        var usuarios = new UsuarioService(_dataStore);

        Assert.Equal(Perfil.Administrativo, usuarios.Login("admin", "admin123")!.Perfil);
        Assert.Equal(Perfil.Veterinario, usuarios.Login("vet", "vet123")!.Perfil);
        Assert.Equal("vet", usuarios.UsuarioLogado!.LoginUsuario);
    }

    [Theory]
    [InlineData("admin", "errada")]
    [InlineData("admin", "ADMIN123")] // senha diferencia maiúsculas
    [InlineData("inexistente", "admin123")]
    [InlineData("", "")]
    public void Login_Invalido_RetornaNulo_ENaoAutentica(string login, string senha)
    {
        var usuarios = new UsuarioService(_dataStore);

        Assert.Null(usuarios.Login(login, senha));
        Assert.Null(usuarios.UsuarioLogado);
    }

    [Fact]
    public void Login_NomeDeUsuario_NaoDiferenciaMaiusculas()
    {
        var usuarios = new UsuarioService(_dataStore);

        Assert.NotNull(usuarios.Login("ADMIN", "admin123"));
    }

    [Fact]
    public void Logout_LimpaUsuarioLogado()
    {
        var usuarios = new UsuarioService(_dataStore);
        usuarios.Login("admin", "admin123");

        usuarios.Logout();

        Assert.Null(usuarios.UsuarioLogado);
    }

    // ---- Dados de exemplo ----

    [Fact]
    public void DadosIniciais_VemDaPlanilhaRealDoCean()
    {
        var inicial = new DataStore();

        Assert.Equal(134, inicial.Produtos.Count);
        Assert.Equal(134, inicial.Produtos.Select(p => p.Nome).Distinct().Count());
        Assert.Equal(4878, inicial.Produtos.Sum(p => p.QuantidadeAtual));
        Assert.Equal(2, inicial.Usuarios.Count);
    }

    [Theory]
    [InlineData("CLORIDRATO DE TRAMADOL - 50 MG - INJETÁVEL, AMPOLA", 269)]
    [InlineData("DIPIRONA - 500 MG - INJETÁVEL, AMPOLA", 63)]
    [InlineData("SULFATO DE MORFINA 10 MG - INJETÁVEL, AMPOLA", 104)]
    [InlineData("MICROCHIP", 400)]
    [InlineData("DIPIRONA 500 MG - ORAL, FRASCO 10 ML", 0)]
    public void DadosIniciais_QuantidadesConferemComAPlanilha(string nome, int esperado)
    {
        var inicial = new DataStore();

        Assert.Equal(esperado, inicial.Produtos.Single(p => p.Nome == nome).QuantidadeAtual);
    }
}
