using SigaCean.Domain;
using SigaCean.Infrastructure;

namespace SigaCean.Services;

public class ProdutoService(DataStore dataStore)
{
    public IReadOnlyList<Produto> ObterTodos() => dataStore.Produtos.Where(p => p.Ativo).ToList();

    public Produto Cadastrar(string nome, CategoriaProduto categoria, string? apresentacaoEspecificacao, int? estoqueMinimo)
    {
        var produto = new Produto
        {
            Nome = nome,
            Categoria = categoria,
            ApresentacaoEspecificacao = apresentacaoEspecificacao,
            EstoqueMinimo = estoqueMinimo
        };
        dataStore.Produtos.Add(produto);
        return produto;
    }

    public void Alterar(Guid produtoId, string nome, CategoriaProduto categoria, string? apresentacaoEspecificacao, int? estoqueMinimo)
    {
        var produto = ObterPorId(produtoId);
        produto.Nome = nome;
        produto.Categoria = categoria;
        produto.ApresentacaoEspecificacao = apresentacaoEspecificacao;
        produto.EstoqueMinimo = estoqueMinimo;
    }

    public Produto ObterPorId(Guid produtoId) =>
        dataStore.Produtos.First(p => p.Id == produtoId);

    // Mitiga duplicidade de nomenclatura por pontuação/espaçamento (ex.: planilha real do CEAN
    // tinha "...INJETÁVEL, AMPOLA" e "...INJETÁVEL - AMPOLA" como produtos distintos).
    public Produto? ObterPorNomeSemelhante(string nome, Guid? ignorarId = null)
    {
        var nomeNormalizado = NormalizarNome(nome);
        return dataStore.Produtos.FirstOrDefault(p =>
            p.Ativo && p.Id != ignorarId && NormalizarNome(p.Nome) == nomeNormalizado);
    }

    private static string NormalizarNome(string nome) =>
        new string(nome.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    // Pesquisa por nome ou categoria (RF0004) — nunca por texto livre de um novo produto (RD0024).
    public IReadOnlyList<Produto> Pesquisar(string? nome, CategoriaProduto? categoria) =>
        dataStore.Produtos.Where(p =>
                p.Ativo &&
                (string.IsNullOrWhiteSpace(nome) || p.Nome.Contains(nome, StringComparison.OrdinalIgnoreCase)) &&
                (categoria is null || p.Categoria == categoria))
            .ToList();
}
