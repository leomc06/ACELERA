using SigaCean.Domain;

namespace SigaCean.Infrastructure;

// Unica fonte de dados do protótipo: tudo em memoria, nada em disco (restrição academica RNF0010).
// Registrado como singleton no MauiProgram para viver durante toda a execução do app.
public class DataStore
{
    public List<Produto> Produtos { get; } = new();
    public List<Movimentacao> Movimentacoes { get; } = new();
    public List<Usuario> Usuarios { get; } = new();

    public DataStore()
    {
        SemearDadosDeExemplo();
    }

    // Usuários são fictícios (só para o protótipo); os produtos e quantidades vêm da planilha real do CEAN.
    private void SemearDadosDeExemplo()
    {
        Usuarios.Add(new Usuario { Nome = "Luiz (exemplo)", LoginUsuario = "admin", Senha = "admin123", Perfil = Perfil.Administrativo });
        Usuarios.Add(new Usuario { Nome = "Dr. Veterinário (exemplo)", LoginUsuario = "vet", Senha = "vet123", Perfil = Perfil.Veterinario });

        // Produtos e quantidades reais da planilha do CEAN (aba "ESTOQUE DE MEDICAMENTOS").
        foreach (var (nome, quantidade) in DadosCean.Medicamentos)
        {
            var categoria = DadosCean.CategoriaDiferenteDeMedicamento.GetValueOrDefault(nome, CategoriaProduto.Medicamento);
            var produto = new Produto { Nome = nome, Categoria = categoria };
            if (quantidade > 0)
                produto.AdicionarQuantidade(quantidade, null);
            Produtos.Add(produto);
        }

        // Historico da aba "ATIVIDADE": so registro, o estoque ja reflete a coluna "EM ESTOQUE".
        Movimentacoes.AddRange(ImportadorHistoricoCean.Importar(Produtos, DadosCean.Historico).Movimentacoes);
    }
}
