using SigaCean.Domain;
using SigaCean.Infrastructure;
using SigaCean.Presentation;
using SigaCean.Services;

namespace SigaCean.Tests;

// Textos mostrados nas telas novas (Estoque, ficha do produto, Saida, Relatorio). As regras continuam nos Services:
// aqui so se confere que a tela formata exatamente o que o dominio decidiu.
public class ApresentacaoTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 9);

    private readonly DataStore _dataStore = new();
    private readonly ProdutoService _produtos;
    private readonly MovimentacaoService _movimentacoes;
    private readonly RelatorioService _relatorio;

    public ApresentacaoTests()
    {
        _dataStore.Produtos.Clear();
        _dataStore.Movimentacoes.Clear();
        _produtos = new ProdutoService(_dataStore);
        _movimentacoes = new MovimentacaoService(_dataStore);
        _relatorio = new RelatorioService(_dataStore);
    }

    // ---- Texto de validade ----

    [Theory]
    [InlineData(41, "vence em 19/11/2026 (41 dias)")]
    [InlineData(1, "vence amanhã (10/10/2026)")]
    [InlineData(0, "vence hoje (09/10/2026)")]
    [InlineData(-1, "venceu em 08/10/2026 (há 1 dia)")]
    [InlineData(-38, "venceu em 01/09/2026 (há 38 dias)")]
    public void DescreverValidade_FormataDiasAteOuDesdeOVencimento(int dias, string esperado)
    {
        Assert.Equal(esperado, TextosDeEstoque.DescreverValidade(Hoje.AddDays(dias), Hoje));
    }

    [Fact]
    public void DescreverSaldo_SemValidade_SaiPorUltimo_ComValidade_DescreveOVencimento()
    {
        Assert.Equal("Sai por último", TextosDeEstoque.DescreverSaldo(null, Hoje));
        Assert.Equal("vence em 19/10/2026 (10 dias)", TextosDeEstoque.DescreverSaldo(Hoje.AddDays(10), Hoje));
    }

    // ---- Textos do produto na lista e na ficha ----

    [Fact]
    public void Categoria_SoMostraApresentacaoQuandoExiste()
    {
        var sem = _produtos.Cadastrar("Ração", CategoriaProduto.Alimentar, null, null);
        var com = _produtos.Cadastrar("Vacina", CategoriaProduto.Medicamento, "Frasco 10 ml", null);

        Assert.Equal("Alimentar", TextosDeEstoque.CategoriaComApresentacao(sem));
        Assert.Equal("Medicamento · Frasco 10 ml", TextosDeEstoque.CategoriaComApresentacao(com));
    }

    [Fact]
    public void MinimoEPercentual_SaemDoDominio()
    {
        var p = _produtos.Cadastrar("Vacina", CategoriaProduto.Medicamento, null, 10);
        _movimentacoes.RegistrarEntrada(p, 8, TipoOrigemEntrada.Compra, null, null, null, null);

        Assert.Equal("mínimo: 10", TextosDeEstoque.Minimo(p));
        Assert.Equal("80% do mínimo", TextosDeEstoque.PercentualDoMinimo(p));
    }

    [Fact]
    public void Percentual_SemMinimoDefinido_FicaVazio()
    {
        var p = _produtos.Cadastrar("Sem minimo", CategoriaProduto.Insumo, null, null);

        Assert.Equal(string.Empty, TextosDeEstoque.PercentualDoMinimo(p));
    }

    // ---- A tela usa as regras do dominio: situacao e proxima validade ----

    [Fact]
    public void ProximaValidade_E_AMaisProximaDoQueExiste_ENaoAUltima()
    {
        var p = _produtos.Cadastrar("Vacina", CategoriaProduto.Medicamento, null, 10);
        _movimentacoes.RegistrarEntrada(p, 4, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(90), null);
        _movimentacoes.RegistrarEntrada(p, 4, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(20), null);

        Assert.Equal(Hoje.AddDays(20), p.ProximaValidade);
        Assert.True(p.TemValidadeProxima(Hoje, EstoqueService.DiasAlertaValidade));
        Assert.Equal("vence em 29/10/2026 (20 dias)", TextosDeEstoque.DescreverValidade(p.ProximaValidade!.Value, Hoje));
    }

    [Fact]
    public void ValidadeVencida_AparecaComoVencido_ENaoComoProxima()
    {
        var p = _produtos.Cadastrar("Vencido", CategoriaProduto.Medicamento, null, null);
        _movimentacoes.RegistrarEntrada(p, 3, TipoOrigemEntrada.Compra, null, null, Hoje.AddDays(-5), null);

        Assert.True(p.TemLoteVencido(Hoje));
        Assert.False(p.TemValidadeProxima(Hoje, EstoqueService.DiasAlertaValidade));
        Assert.Equal("venceu em 04/10/2026 (há 5 dias)", TextosDeEstoque.DescreverValidade(p.ProximaValidade!.Value, Hoje));
    }

    // ---- Previsao FEFO exibida na Saida ----

    [Fact]
    public void Previsao_DescreveDeOndeSaiOEstoque()
    {
        var texto = Rotulos.Previsao(
        [
            new LoteConsumido(new DateOnly(2026, 11, 12), 4),
            new LoteConsumido(null, 3)
        ]);

        Assert.Equal("4 un. com validade 12/11/2026; 3 un. sem validade informada", texto);
    }

    // ---- Dica do Relatorio ----

    [Fact]
    public void UltimaMovimentacao_SemHistorico_ENula()
    {
        Assert.Null(_relatorio.DataDaUltimaMovimentacao());
    }

    [Fact]
    public void UltimaMovimentacao_EAMaisRecente()
    {
        var p = _produtos.Cadastrar("Produto", CategoriaProduto.Insumo, null, null);
        var primeira = _movimentacoes.RegistrarEntrada(p, 5, TipoOrigemEntrada.Compra, null, null, null, null);
        var segunda = _movimentacoes.RegistrarSaida(p, 1, "Canil", null, "Maria");

        Assert.Equal(new[] { primeira.Data, segunda.Data }.Max(), _relatorio.DataDaUltimaMovimentacao());
    }
}
