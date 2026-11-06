using SigaCean.Domain;
using SigaCean.Infrastructure;
using SigaCean.Services;

namespace SigaCean.Tests;

// Pedido do orientador do CEAN: o Veterinario tambem registra saida; as telas seguem as regras de Domain/Permissoes.
public class PermissoesPorPerfilTests
{
    private readonly DataStore _dataStore = new();
    private readonly MovimentacaoService _movimentacoes;
    private readonly ProdutoService _produtos;

    public PermissoesPorPerfilTests()
    {
        _dataStore.Produtos.Clear();
        _dataStore.Movimentacoes.Clear();
        _movimentacoes = new MovimentacaoService(_dataStore);
        _produtos = new ProdutoService(_dataStore);
    }

    private static Usuario NovoUsuario(Perfil perfil) =>
        new() { Nome = perfil.ToString(), LoginUsuario = perfil.ToString().ToLower(), Senha = "x", Perfil = perfil };

    [Fact]
    public void Veterinario_RegistraSaida_ENaoPodeAdministrar()
    {
        var vet = NovoUsuario(Perfil.Veterinario);

        Assert.True(Permissoes.PodeRegistrarSaida(vet));
        Assert.False(Permissoes.PodeAdministrar(vet));
    }

    [Fact]
    public void Administrativo_RegistraSaida_EAdministra()
    {
        var adm = NovoUsuario(Perfil.Administrativo);

        Assert.True(Permissoes.PodeRegistrarSaida(adm));
        Assert.True(Permissoes.PodeAdministrar(adm));
    }

    [Fact]
    public void SemUsuarioLogado_NaoHaAcoes()
    {
        Assert.False(Permissoes.PodeRegistrarSaida(null));
        Assert.False(Permissoes.PodeAdministrar(null));
    }

    [Fact]
    public void SaidaDoVeterinario_SubtraiDoEstoque_ERegistraQuemFez()
    {
        var vet = NovoUsuario(Perfil.Veterinario);
        var p = _produtos.Cadastrar("Produto Teste", CategoriaProduto.Medicamento, null, 5);
        _movimentacoes.RegistrarEntrada(p, 10, TipoOrigemEntrada.Compra, null, null, null, null);

        var saida = _movimentacoes.RegistrarSaida(p, 4, "Canil", vet, "Maria");

        Assert.Equal(6, p.QuantidadeAtual);
        Assert.Same(vet, saida.UsuarioResponsavel);
        Assert.Equal("Maria", saida.RetiradoPor);
    }

    [Fact]
    public void SaidaDoVeterinario_AcimaDoSaldo_ContinuaRecusada()
    {
        var vet = NovoUsuario(Perfil.Veterinario);
        var p = _produtos.Cadastrar("Produto Teste", CategoriaProduto.Medicamento, null, 5);
        _movimentacoes.RegistrarEntrada(p, 3, TipoOrigemEntrada.Compra, null, null, null, null);

        Assert.Throws<InvalidOperationException>(() => _movimentacoes.RegistrarSaida(p, 4, "Canil", vet, "Maria"));
        Assert.Equal(3, p.QuantidadeAtual);
    }
}
