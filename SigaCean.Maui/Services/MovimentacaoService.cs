using SigaCean.Domain;
using SigaCean.Infrastructure;

namespace SigaCean.Services;

public class MovimentacaoService(DataStore dataStore)
{
    public EntradaEstoque RegistrarEntrada(
        Produto produto, int quantidade, TipoOrigemEntrada tipoOrigem,
        string? numeroAF, string? detalheOrigem, string? lote, DateOnly? dataValidade, Usuario? responsavel,
        string? documento = null)
    {
        var entrada = new EntradaEstoque
        {
            Produto = produto,
            Quantidade = quantidade,
            TipoOrigem = tipoOrigem,
            NumeroAF = numeroAF,
            Documento = documento,
            DetalheOrigem = detalheOrigem,
            Lote = lote,
            DataValidade = dataValidade,
            UsuarioResponsavel = responsavel
        };

        produto.AdicionarQuantidade(quantidade, lote, dataValidade);
        dataStore.Movimentacoes.Add(entrada);
        return entrada;
    }

    public SaidaEstoque RegistrarSaida(Produto produto, int quantidade, string destino, Usuario? responsavel, string? retiradoPor = null)
    {
        var consumidos = produto.ConsumirQuantidade(quantidade); // FEFO: consome primeiro o lote que vence mais cedo

        var saida = new SaidaEstoque
        {
            Produto = produto,
            Quantidade = quantidade,
            Destino = destino,
            RetiradoPor = retiradoPor,
            UsuarioResponsavel = responsavel
        };
        saida.LotesConsumidos.AddRange(consumidos);
        dataStore.Movimentacoes.Add(saida);
        return saida;
    }

    public AjusteBalanco RegistrarAjusteBalanco(Produto produto, int quantidadeApurada, Usuario? responsavel)
    {
        var quantidadeAnterior = produto.QuantidadeAtual;
        var diferenca = quantidadeApurada - quantidadeAnterior;

        IReadOnlyList<LoteConsumido> consumidos = [];
        if (diferenca > 0)
            produto.AdicionarQuantidade(diferenca, null, null);
        else if (diferenca < 0)
            consumidos = produto.ConsumirQuantidade(-diferenca);

        var ajuste = new AjusteBalanco
        {
            Produto = produto,
            Quantidade = diferenca,
            QuantidadeAnterior = quantidadeAnterior,
            QuantidadeApurada = quantidadeApurada,
            UsuarioResponsavel = responsavel
        };
        ajuste.LotesConsumidos.AddRange(consumidos);
        dataStore.Movimentacoes.Add(ajuste);
        return ajuste;
    }

    // Corrigir e cancelar desfazem o efeito da movimentacao no estoque; a movimentacao fica no historico com o motivo.
    public void Corrigir(Guid movimentacaoId, string motivo) =>
        Desfazer(movimentacaoId, motivo, StatusMovimentacao.Corrigida);

    public void Cancelar(Guid movimentacaoId, string motivo) =>
        Desfazer(movimentacaoId, motivo, StatusMovimentacao.Cancelada);

    private void Desfazer(Guid movimentacaoId, string motivo, StatusMovimentacao novoStatus)
    {
        var movimentacao = dataStore.Movimentacoes.First(m => m.Id == movimentacaoId);

        if (movimentacao.Status != StatusMovimentacao.Ativa)
            throw new InvalidOperationException("Somente movimentacoes ativas podem ser corrigidas ou canceladas.");

        ReverterEfeitoNoEstoque(movimentacao); // pode falhar (estoque da entrada ja usado): nada muda nesse caso

        movimentacao.Status = novoStatus;
        movimentacao.MotivoCorrecaoOuCancelamento = motivo;
    }

    private static void ReverterEfeitoNoEstoque(Movimentacao movimentacao)
    {
        switch (movimentacao)
        {
            case EntradaEstoque entrada:
                entrada.Produto.RemoverDoLote(entrada.Lote, entrada.DataValidade, entrada.Quantidade);
                break;
            case SaidaEstoque saida:
                saida.Produto.DevolverLotes(saida.LotesConsumidos);
                break;
            case AjusteBalanco ajuste when ajuste.Quantidade > 0:
                ajuste.Produto.RemoverDoLote(null, null, ajuste.Quantidade);
                break;
            case AjusteBalanco ajuste when ajuste.Quantidade < 0:
                ajuste.Produto.DevolverLotes(ajuste.LotesConsumidos);
                break;
        }
    }

    public IReadOnlyList<Movimentacao> ObterHistorico(Produto? produto = null, DateTime? dataInicio = null, DateTime? dataFim = null) =>
        dataStore.Movimentacoes
            .Where(m => produto is null || m.Produto.Id == produto.Id)
            .Where(m => dataInicio is null || m.Data >= dataInicio)
            .Where(m => dataFim is null || m.Data <= dataFim)
            .OrderByDescending(m => m.Data)
            .ToList();
}
