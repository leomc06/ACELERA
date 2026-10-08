using SigaCean.Domain;
using SigaCean.Infrastructure;

namespace SigaCean.Services;

public class ProdutoService(DataStore dataStore)
{
    public IReadOnlyList<Produto> ObterTodos() => dataStore.Produtos.Where(p => p.Ativo).ToList();

    public Produto Cadastrar(string nome, CategoriaProduto categoria, string? apresentacaoEspecificacao, int? estoqueMinimo)
    {
        ValidarDados(nome, estoqueMinimo);

        var produto = new Produto
        {
            Nome = nome.Trim(),
            Categoria = categoria,
            ApresentacaoEspecificacao = LimparTexto(apresentacaoEspecificacao),
            EstoqueMinimo = estoqueMinimo
        };
        dataStore.Produtos.Add(produto);
        return produto;
    }

    public void Alterar(Guid produtoId, string nome, CategoriaProduto categoria, string? apresentacaoEspecificacao, int? estoqueMinimo)
    {
        ValidarDados(nome, estoqueMinimo);

        var produto = ObterPorId(produtoId);
        produto.Nome = nome.Trim();
        produto.Categoria = categoria;
        produto.ApresentacaoEspecificacao = LimparTexto(apresentacaoEspecificacao);
        produto.EstoqueMinimo = estoqueMinimo;
    }

    private static void ValidarDados(string nome, int? estoqueMinimo)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new InvalidOperationException("Informe o nome do produto.");
        if (estoqueMinimo < 0)
            throw new InvalidOperationException("O estoque mínimo não pode ser negativo.");
    }

    private static string? LimparTexto(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

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
                BuscaTexto.Contem(p.Nome, nome) &&
                (categoria is null || p.Categoria == categoria))
            .ToList();
}
