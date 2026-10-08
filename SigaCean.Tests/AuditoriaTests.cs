using SigaCean.Domain;
using SigaCean.Infrastructure;
using SigaCean.Services;

namespace SigaCean.Tests;

// Casos levantados na auditoria: o servico precisa se proteger sozinho, sem depender da validacao das telas.
public class AuditoriaTests
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);

    private readonly DataStore _dataStore = new();
    private readonly MovimentacaoService _movimentacoes;
    private readonly ProdutoService _produtos;
    private readonly EstoqueService _estoque;

    public AuditoriaTests()
    {
        _dataStore.Produtos.Clear();
        _dataStore.Movimentacoes.Clear();
        _movimentacoes = new MovimentacaoService(_dataStore);
        _produtos = new ProdutoService(_dataStore);
        _estoque = new EstoqueService(_dataStore);
    }

    private Produto NovoProduto(int? minimo = 10) =>
        _produtos.Cadastrar("Produto Teste", CategoriaProduto.Medicamento, null, minimo);

    // ---- Quantidade invalida nao pode alterar nada ----

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Entrada_ComQuantidadeZeroOuNegativa_Falha_ENadaMuda(int quantidade)
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, null, null);

        Assert.Throws<InvalidOperationException>(() =>
            _movimentacoes.RegistrarEntrada(p, quantidade, TipoOrigemEntrada.Compra, null, null, null, null));

        Assert.Equal(10, p.QuantidadeAtual);
        Assert.Single(_dataStore.Movimentacoes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Saida_ComQuantidadeZeroOuNegativa_Falha_ENadaMuda(int quantidade)
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, null, null);

        Assert.Throws<InvalidOperationException>(() => _movimentacoes.RegistrarSaida(p, quantidade, "Canil", null));

        Assert.Equal(10, p.QuantidadeAtual);
        Assert.Single(_dataStore.Movimentacoes);
    }

    [Fact]
    public void Saida_SemDestino_Falha_ENadaMuda()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, null, null);

        Assert.Throws<InvalidOperationException>(() => _movimentacoes.RegistrarSaida(p, 2, "  ", null));

        Assert.Equal(10, p.QuantidadeAtual);
        Assert.Single(_dataStore.Movimentacoes);
    }

    [Fact]
    public void Ajuste_ComQuantidadeApuradaNegativa_Falha_ENadaMuda()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, null, null);

        Assert.Throws<InvalidOperationException>(() => _movimentacoes.RegistrarAjusteBalanco(p, -1, null));

        Assert.Equal(10, p.QuantidadeAtual);
        Assert.Single(_dataStore.Movimentacoes);
    }

    // ---- Produto inativo nao recebe movimentacao ----

    [Fact]
    public void ProdutoInativo_NaoRecebeEntradaNemSaida()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, null, null);
        p.Ativo = false;

        Assert.Throws<InvalidOperationException>(() =>
            _movimentacoes.RegistrarEntrada(p, 1, TipoOrigemEntrada.Compra, null, null, null, null));
        Assert.Throws<InvalidOperationException>(() => _movimentacoes.RegistrarSaida(p, 1, "Canil", null));

        Assert.Equal(10, p.QuantidadeAtual);
    }

    // ---- Validade so vale para o que ainda existe em estoque ----

    [Fact]
    public void Validade_LoteJaTotalmenteConsumido_NaoMarcaProdutoComoVencido()
    {
        var p = NovoProduto();
        var passado = Hoje.AddDays(-10);
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, passado, null);
        _movimentacoes.RegistrarSaida(p, 10, "Canil", null);

        Assert.Equal(0, p.QuantidadeAtual);
        Assert.False(p.TemLoteVencido(Hoje));
        Assert.DoesNotContain(p, _estoque.Filtrar(null, null, SituacaoEstoque.Vencido, Hoje));
    }

    [Fact]
    public void Validade_LoteZeradoComValidadeProxima_NaoAlerta()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(5), null);
        _movimentacoes.RegistrarSaida(p, 10, "Canil", null);

        Assert.False(p.TemLoteProximoDoVencimento(Hoje, EstoqueService.DiasAlertaValidade));
    }

    // ---- Uma unica regra de validade: a lista e os contadores concordam ----

    [Fact]
    public void ObterProximosDoVencimento_NaoInclui_ProdutoJaVencido()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 5, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(-1), null);
        _movimentacoes.RegistrarEntrada(p, 5, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(5), null);

        Assert.Contains(p, _estoque.ObterVencidos(Hoje));
        Assert.DoesNotContain(p, _estoque.ObterProximosDoVencimento(Hoje));
        Assert.Equal(
            _estoque.Filtrar(null, null, SituacaoEstoque.ValidadeProxima, Hoje).Count,
            _estoque.ObterProximosDoVencimento(Hoje).Count);
    }

    // ---- Produto: nome com espacos sobrando ----

    [Fact]
    public void Cadastrar_AparaOsEspacosDoNomeEDaApresentacao()
    {
        var p = _produtos.Cadastrar("  DIPIRONA 500 MG  ", CategoriaProduto.Medicamento, "  ampola  ", 5);

        Assert.Equal("DIPIRONA 500 MG", p.Nome);
        Assert.Equal("ampola", p.ApresentacaoEspecificacao);
    }

    [Fact]
    public void Cadastrar_SemNome_Falha()
    {
        Assert.Throws<InvalidOperationException>(() => _produtos.Cadastrar("   ", CategoriaProduto.Insumo, null, null));
        Assert.Empty(_dataStore.Produtos);
    }

    [Fact]
    public void Cadastrar_ComEstoqueMinimoNegativo_Falha()
    {
        Assert.Throws<InvalidOperationException>(() => _produtos.Cadastrar("X", CategoriaProduto.Insumo, null, -1));
        Assert.Empty(_dataStore.Produtos);
    }

    // ---- Etapa 1: "sem estoque" vale com ou sem estoque minimo ----

    [Theory]
    [InlineData(null)]
    [InlineData(10)]
    public void SemEstoque_QuantidadeZero_EVermelho_ComOuSemMinimo(int? minimo)
    {
        var p = NovoProduto(minimo);

        Assert.True(p.EstaSemEstoque);
        Assert.Equal(CorSaudeEstoque.Vermelho, p.CorSaude);
    }

    [Fact]
    public void SemEstoque_ComQuantidade_NaoEstaSemEstoque_ESemMinimoContinuaVerde()
    {
        var p = NovoProduto(minimo: null);
        _movimentacoes.RegistrarEntrada(p, 1, TipoOrigemEntrada.Compra, null, null, null, null);

        Assert.False(p.EstaSemEstoque);
        Assert.Equal(CorSaudeEstoque.Verde, p.CorSaude);
    }

    [Fact]
    public void SemEstoque_AposSaidaTotal_EntraNoFiltro_ESaiAposNovaEntrada()
    {
        var p = NovoProduto(minimo: null);
        _movimentacoes.RegistrarEntrada(p, 4, TipoOrigemEntrada.Compra, null, null, null, null);
        Assert.DoesNotContain(p, _estoque.Filtrar(null, null, SituacaoEstoque.SemEstoque));

        _movimentacoes.RegistrarSaida(p, 4, "Canil", null);
        Assert.Contains(p, _estoque.Filtrar(null, null, SituacaoEstoque.SemEstoque));
        Assert.Equal(1, _estoque.Contar(SituacaoEstoque.SemEstoque));

        _movimentacoes.RegistrarEntrada(p, 1, TipoOrigemEntrada.Compra, null, null, null, null);
        Assert.DoesNotContain(p, _estoque.Filtrar(null, null, SituacaoEstoque.SemEstoque));
    }

    [Fact]
    public void SemEstoque_DadosReaisDoCean_ContadorEFiltroConcordam()
    {
        var real = new DataStore();
        var estoque = new EstoqueService(real);
        var zerados = real.Produtos.Count(p => p.QuantidadeAtual == 0);

        Assert.True(zerados > 0); // a planilha real tem produtos zerados
        Assert.Equal(zerados, estoque.Contar(SituacaoEstoque.SemEstoque));
        Assert.Equal(zerados, estoque.Filtrar(null, null, SituacaoEstoque.SemEstoque).Count);
    }

    // ---- Etapa 6: sem codigo de lote, o estoque se agrupa pela validade ----

    [Fact]
    public void Validade_EntradasComValidadesDiferentes_FicamSeparadas_EIguaisSomam()
    {
        var p = NovoProduto();
        var cedo = Hoje.AddDays(10);
        var tarde = Hoje.AddDays(90);

        _movimentacoes.RegistrarEntrada(p, 5, TipoOrigemEntrada.Compra, null, null, cedo, null);
        _movimentacoes.RegistrarEntrada(p, 7, TipoOrigemEntrada.Compra, null, null, tarde, null);
        _movimentacoes.RegistrarEntrada(p, 3, TipoOrigemEntrada.Compra, null, null, cedo, null);

        Assert.Equal(2, p.Lotes.Count);
        Assert.Equal(8, p.Lotes.Single(l => l.DataValidade == cedo).Quantidade);
        Assert.Equal(7, p.Lotes.Single(l => l.DataValidade == tarde).Quantidade);
        Assert.Equal(15, p.QuantidadeAtual);
    }

    [Fact]
    public void Validade_EntradasSemValidade_SomamNoMesmoSaldo()
    {
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 5, TipoOrigemEntrada.Compra, null, null, null, null);
        _movimentacoes.RegistrarEntrada(p, 4, TipoOrigemEntrada.Doacao, null, null, null, null);

        var saldo = Assert.Single(p.Lotes);
        Assert.Equal(9, saldo.Quantidade);
        Assert.Null(saldo.DataValidade);
    }

    [Fact]
    public void Cancelar_EntradaDeMesmaValidade_SoSeAindaHaSaldoSuficiente()
    {
        var p = NovoProduto();
        var validade = Hoje.AddDays(20);
        var primeira = _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, validade, null);
        var segunda = _movimentacoes.RegistrarEntrada(p, 5, TipoOrigemEntrada.Compra, null, null, validade, null);
        _movimentacoes.RegistrarSaida(p, 8, "Canil", null); // sobram 7 dessa validade

        Assert.Throws<InvalidOperationException>(() => _movimentacoes.Cancelar(primeira.Id, "Engano"));
        Assert.Equal(7, p.QuantidadeAtual); // falhou sem alterar nada

        _movimentacoes.Cancelar(segunda.Id, "Engano");
        Assert.Equal(2, p.QuantidadeAtual);
    }

    [Fact]
    public void Historico_DaEntrada_NaoMostraCodigoDeLote()
    {
        Assert.Null(typeof(EntradaEstoque).GetProperty("Lote"));
        Assert.Null(typeof(LoteEstoque).GetProperty("Codigo"));
    }

    // ---- Categorias definidas pelo CEAN para os itens que nao sao remedio ----

    [Theory]
    [InlineData("MICROCHIP", CategoriaProduto.Microchip)]
    [InlineData("PAPINHA DE DESMAME 1KG", CategoriaProduto.Alimentar)]
    [InlineData("SHAMPOO, CLOREXIDINA 20 G - FRASCO 500 ML", CategoriaProduto.Higiene)]
    [InlineData("SHAMPOO, CETOCONAZOL 0,5 G + CLOREXIDINA 20 G - FRASCO 500 ML", CategoriaProduto.Higiene)]
    [InlineData("TESTE DE CINOMOSE", CategoriaProduto.Medicamento)]
    [InlineData("POMADA HIDRATANTE - RICINIUS 100 G - FRASCO 250 G", CategoriaProduto.Medicamento)]
    [InlineData("SUPLEMENTO SUBSTITUTO DE LEITE MATERNO, 300 G, P/CÃES E GATOS", CategoriaProduto.Medicamento)]
    public void DadosIniciais_CategoriaDosItensQueNaoSaoRemedio(string nome, CategoriaProduto esperada)
    {
        var inicial = new DataStore();

        Assert.Equal(esperada, inicial.Produtos.Single(p => p.Nome == nome).Categoria);
    }

    [Fact]
    public void DadosIniciais_SoQuatroProdutosForamDeMedicamento_ETodasAsChavesExistem()
    {
        var inicial = new DataStore();
        var nomes = inicial.Produtos.Select(p => p.Nome).ToHashSet();

        Assert.All(DadosCean.CategoriaDiferenteDeMedicamento.Keys, chave => Assert.Contains(chave, nomes));
        Assert.Equal(4, inicial.Produtos.Count(p => p.Categoria != CategoriaProduto.Medicamento));
    }

    [Fact]
    public void Categorias_NovasAparecemNosFiltrosENoRelatorio()
    {
        var inicial = new DataStore();
        var estoque = new EstoqueService(inicial);

        var microchip = Assert.Single(estoque.Filtrar(null, CategoriaProduto.Microchip, SituacaoEstoque.Todos));
        Assert.Equal(400, microchip.QuantidadeAtual);
        Assert.Equal(2, estoque.Filtrar(null, CategoriaProduto.Higiene, SituacaoEstoque.Todos).Count);

        Assert.Equal("Microchip", Presentation.Rotulos.Categoria(CategoriaProduto.Microchip));
        Assert.Equal("Higiene", Presentation.Rotulos.Categoria(CategoriaProduto.Higiene));
        Assert.Contains(Presentation.Rotulos.CategoriasParaFiltro, c => c.Valor == CategoriaProduto.Microchip);
        Assert.Contains(Presentation.Rotulos.CategoriasParaFiltro, c => c.Valor == CategoriaProduto.Higiene);
    }

    // ---- Etapa 8: textos de exibicao definidos uma vez ----

    [Fact]
    public void Rotulos_TodaCategoriaTemTextoEApareceNaListaDeEscolha()
    {
        foreach (var categoria in Enum.GetValues<CategoriaProduto>())
        {
            Assert.False(string.IsNullOrWhiteSpace(Presentation.Rotulos.Categoria(categoria)));
            Assert.Contains(categoria, Presentation.Rotulos.Categorias);
        }

        Assert.Equal("Equipamento hospitalar", Presentation.Rotulos.Categoria(CategoriaProduto.EquipamentoHospitalar));
        Assert.Equal(Presentation.Rotulos.Categorias.Length + 1, Presentation.Rotulos.CategoriasParaFiltro.Length);
        Assert.Null(Presentation.Rotulos.CategoriasParaFiltro[0].Valor);
        Assert.Equal("Todas as categorias", Presentation.Rotulos.CategoriasParaFiltro[0].Texto);
    }

    [Fact]
    public void Rotulos_Perfil()
    {
        Assert.Equal("Administrativo", Presentation.Rotulos.Perfil(Perfil.Administrativo));
        Assert.Equal("Veterinário", Presentation.Rotulos.Perfil(Perfil.Veterinario));
        Assert.Equal(string.Empty, Presentation.Rotulos.Perfil(null));
    }

    // ---- Etapa 5: mensagem de sucesso ----

    [Fact]
    public void Aviso_EntregaAMensagemUmaVezSo()
    {
        var aviso = new AvisoService();
        Assert.Null(aviso.Consumir());

        aviso.Publicar("Saída registrada.");

        Assert.Equal("Saída registrada.", aviso.Consumir());
        Assert.Null(aviso.Consumir());
    }

    [Fact]
    public void Aviso_NovaMensagemSubstituiAAnteriorNaoLida()
    {
        var aviso = new AvisoService();
        aviso.Publicar("primeira");
        aviso.Publicar("segunda");

        Assert.Equal("segunda", aviso.Consumir());
    }

    // ---- Etapa 4: filtros do historico ----

    private (Produto Produto, EntradaEstoque Entrada, SaidaEstoque Saida, AjusteBalanco Ajuste) HistoricoDeExemplo()
    {
        var p = NovoProduto();
        var entrada = _movimentacoes.RegistrarEntrada(p, 20, TipoOrigemEntrada.Compra, null, null, null, null);
        var saida = _movimentacoes.RegistrarSaida(p, 5, "Canil", null);
        var ajuste = _movimentacoes.RegistrarAjusteBalanco(p, 10, null);
        return (p, entrada, saida, ajuste);
    }

    [Fact]
    public void Historico_FiltraPorTipo()
    {
        var (_, entrada, saida, ajuste) = HistoricoDeExemplo();

        Assert.Equal([entrada], _movimentacoes.ObterHistorico(tipo: TipoMovimentacao.Entrada));
        Assert.Equal([saida], _movimentacoes.ObterHistorico(tipo: TipoMovimentacao.Saida));
        Assert.Equal([ajuste], _movimentacoes.ObterHistorico(tipo: TipoMovimentacao.Ajuste));
        Assert.Equal(3, _movimentacoes.ObterHistorico().Count);
    }

    [Fact]
    public void Historico_FiltraPorSituacao_EmCombinacaoComTipo()
    {
        var (_, entrada, saida, _) = HistoricoDeExemplo();
        _movimentacoes.Cancelar(saida.Id, "Digitada errado");

        Assert.Equal([saida], _movimentacoes.ObterHistorico(status: StatusMovimentacao.Cancelada));
        Assert.Equal(2, _movimentacoes.ObterHistorico(status: StatusMovimentacao.Ativa).Count);
        Assert.Empty(_movimentacoes.ObterHistorico(tipo: TipoMovimentacao.Entrada, status: StatusMovimentacao.Cancelada));
        Assert.Equal([entrada], _movimentacoes.ObterHistorico(tipo: TipoMovimentacao.Entrada, status: StatusMovimentacao.Ativa));
    }

    [Fact]
    public void Historico_Periodo_IncluiODiaFinalPorInteiro()
    {
        var (_, entrada, _, _) = HistoricoDeExemplo();
        var dia = DateTime.Today;
        var fimDoDia = dia.AddDays(1).AddTicks(-1); // como a tela faz

        Assert.Contains(entrada, _movimentacoes.ObterHistorico(dataInicio: dia, dataFim: fimDoDia));
        Assert.Empty(_movimentacoes.ObterHistorico(dataInicio: dia.AddDays(-10), dataFim: dia.AddDays(-1).AddTicks(-1)));
        Assert.Empty(_movimentacoes.ObterHistorico(dataInicio: dia.AddDays(1)));
    }

    [Fact]
    public void Historico_Filtros_ValemTambemParaOHistoricoImportadoDoCean()
    {
        var real = new DataStore();
        var servico = new MovimentacaoService(real);

        var total = servico.ObterHistorico().Count;
        var porTipo = Enum.GetValues<TipoMovimentacao>().Sum(t => servico.ObterHistorico(tipo: t).Count);

        Assert.True(total > 0);
        Assert.Equal(total, porTipo); // todo registro cai em exatamente um tipo
    }

    // ---- Etapa 3: busca ignora maiusculas e acentos ----

    [Theory]
    [InlineData("INJETÁVEL, AMPOLA", "injetavel", true)]
    [InlineData("INJETÁVEL, AMPOLA", "INJETÁVEL", true)]
    [InlineData("injetavel", "INJETÁVEL", true)]
    [InlineData("CLORIDRATO DE LIDOICAÍNA", "  lidoicaina ", true)]
    [InlineData("AÇÚCAR", "acucar", true)]
    [InlineData("DIPIRONA", "", true)]
    [InlineData("DIPIRONA", null, true)]
    [InlineData("DIPIRONA", "tramadol", false)]
    [InlineData("", "dipirona", false)]
    public void BuscaTexto_IgnoraMaiusculasEAcentos(string texto, string? termo, bool esperado) =>
        Assert.Equal(esperado, BuscaTexto.Contem(texto, termo));

    [Fact]
    public void Busca_EstoqueEProdutos_AchamSemDigitarAcento()
    {
        var p = _produtos.Cadastrar("DIPIRONA 500 MG - INJETÁVEL, AMPOLA", CategoriaProduto.Medicamento, null, null);

        Assert.Contains(p, _estoque.Filtrar("injetavel", null, SituacaoEstoque.Todos));
        Assert.Contains(p, _produtos.Pesquisar("INJETAVEL", null));
        Assert.Empty(_estoque.Filtrar("comprimido", null, SituacaoEstoque.Todos));
    }

    [Fact]
    public void Busca_DadosReais_AcentoNaoMudaOResultado()
    {
        var real = new DataStore();
        var estoque = new EstoqueService(real);

        var comAcento = estoque.Filtrar("INJETÁVEL", null, SituacaoEstoque.Todos).Count;
        var semAcento = estoque.Filtrar("injetavel", null, SituacaoEstoque.Todos).Count;

        Assert.True(comAcento > 0);
        Assert.Equal(comAcento, semAcento);
    }

    // ---- Etapa 2: sair e entrar com outro usuario ----

    [Fact]
    public void Sair_ePermiteEntrarComOutroUsuario_SemPerderOsDados()
    {
        var usuarios = new UsuarioService(_dataStore);
        _dataStore.Usuarios.Add(new Usuario { Nome = "Adm", LoginUsuario = "adm", Senha = "1", Perfil = Perfil.Administrativo });
        _dataStore.Usuarios.Add(new Usuario { Nome = "Vet", LoginUsuario = "vet2", Senha = "2", Perfil = Perfil.Veterinario });
        var p = NovoProduto();
        _movimentacoes.RegistrarEntrada(p, 3, TipoOrigemEntrada.Compra, null, null, null, null);

        Assert.Equal(Perfil.Administrativo, usuarios.Login("adm", "1")!.Perfil);
        usuarios.Logout();
        Assert.Null(usuarios.UsuarioLogado);

        Assert.Equal(Perfil.Veterinario, usuarios.Login("vet2", "2")!.Perfil);
        Assert.Equal(3, p.QuantidadeAtual);
    }

    // ---- Estoque minimo: exatamente no minimo ja e "estoque baixo" ----

    [Fact]
    public void EstoqueMinimo_AbaixoIgualEAcima()
    {
        var p = NovoProduto(minimo: 10);
        _movimentacoes.RegistrarEntrada(p, 11, TipoOrigemEntrada.Compra, null, null, null, null);
        Assert.False(p.EstaAbaixoDoMinimo);

        _movimentacoes.RegistrarSaida(p, 1, "Canil", null); // 10 = minimo
        Assert.True(p.EstaAbaixoDoMinimo);

        _movimentacoes.RegistrarSaida(p, 1, "Canil", null); // 9 < minimo
        Assert.True(p.EstaAbaixoDoMinimo);
    }
}
