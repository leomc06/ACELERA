using SigaCean.Domain;
using SigaCean.Infrastructure;

namespace SigaCean.Services;

public class ProdutoService(DataStore dataStore)
{
    public IReadOnlyList<Produto> ObterTodos() => dataStore.Produtos.Where(p => p.Ativo).ToList();

    public Produto Cadastrar(string nome, CategoriaProduto categoria, string? apresentacaoEspecificacao, int? estoqueMinimo)
    {
        ValidarDados(nome, estoqueMinimo);
        ValidarSemDuplicidade(nome, apresentacaoEspecificacao, null);

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
        ValidarSemDuplicidade(nome, apresentacaoEspecificacao, produtoId);

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

    // Produto identico = mesmo nome e mesma apresentacao (sem diferenciar maiusculas). O service recusa esse caso; nomes
    // apenas PARECIDOS (pontuacao diferente) continuam permitidos: a tela avisa e deixa o usuario decidir (ObterPorNomeSemelhante).
    public bool ExisteIdentico(string nome, string? apresentacao, Guid? ignorarId = null) =>
        dataStore.Produtos.Any(p =>
            p.Id != ignorarId &&
            string.Equals(p.Nome.Trim(), nome.Trim(), StringComparison.CurrentCultureIgnoreCase) &&
            string.Equals(LimparTexto(p.ApresentacaoEspecificacao) ?? "", LimparTexto(apresentacao) ?? "", StringComparison.CurrentCultureIgnoreCase));

    private void ValidarSemDuplicidade(string nome, string? apresentacao, Guid? ignorarId)
    {
        if (ExisteIdentico(nome, apresentacao, ignorarId))
            throw new InvalidOperationException($"Já existe um produto \"{nome.Trim()}\" com a mesma apresentação.");
    }

    private static string? LimparTexto(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    public Produto ObterPorId(Guid produtoId) =>
        dataStore.Produtos.FirstOrDefault(p => p.Id == produtoId)
            ?? throw new InvalidOperationException("Produto não encontrado.");

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
