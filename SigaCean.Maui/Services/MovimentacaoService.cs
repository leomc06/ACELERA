using SigaCean.Domain;
using SigaCean.Infrastructure;

namespace SigaCean.Services;

public class MovimentacaoService(DataStore dataStore)
{
    public EntradaEstoque RegistrarEntrada(
        Produto produto, int quantidade, TipoOrigemEntrada tipoOrigem,
        string? numeroAF, string? detalheOrigem, DateOnly? dataValidade, Usuario? responsavel,
        string? documento = null)
    {
        ValidarMovimentacao(produto, quantidade);

        var entrada = new EntradaEstoque
        {
            Produto = produto,
            Quantidade = quantidade,
            TipoOrigem = tipoOrigem,
            NumeroAF = numeroAF,
            Documento = documento,
            DetalheOrigem = detalheOrigem,
            DataValidade = dataValidade,
            UsuarioResponsavel = responsavel
        };

        produto.AdicionarQuantidade(quantidade, dataValidade);
        dataStore.Movimentacoes.Add(entrada);
        return entrada;
    }

    public SaidaEstoque RegistrarSaida(Produto produto, int quantidade, string destino, Usuario? responsavel, string retiradoPor)
    {
        ValidarMovimentacao(produto, quantidade);
        if (string.IsNullOrWhiteSpace(destino))
            throw new InvalidOperationException("Informe o destino da saída.");
        if (string.IsNullOrWhiteSpace(retiradoPor))
            throw new InvalidOperationException("Informe quem está retirando o produto.");

        var consumidos = produto.ConsumirQuantidade(quantidade); // FEFO: sai primeiro o que vence mais cedo

        var saida = new SaidaEstoque
        {
            Produto = produto,
            Quantidade = quantidade,
            Destino = destino.Trim(),
            RetiradoPor = retiradoPor.Trim(),
            UsuarioResponsavel = responsavel
        };
        saida.LotesConsumidos.AddRange(consumidos);
        dataStore.Movimentacoes.Add(saida);
        return saida;
    }

    public AjusteBalanco RegistrarAjusteBalanco(Produto produto, int quantidadeApurada, Usuario? responsavel)
    {
        if (!produto.Ativo)
            throw new InvalidOperationException($"O produto '{produto.Nome}' está inativo e não aceita movimentações.");
        if (quantidadeApurada < 0)
            throw new InvalidOperationException("A quantidade apurada não pode ser negativa.");

        var quantidadeAnterior = produto.QuantidadeAtual;
        var diferenca = quantidadeApurada - quantidadeAnterior;

        IReadOnlyList<LoteConsumido> consumidos = [];
        if (diferenca > 0)
            produto.AdicionarQuantidade(diferenca, null);
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

    // Mostra, sem alterar nada, de quais validades uma saida retiraria a quantidade (FEFO). Usa as mesmas regras da saida.
    public IReadOnlyList<LoteConsumido> PrevisaoDeSaida(Produto produto, int quantidade)
    {
        ValidarMovimentacao(produto, quantidade);
        return produto.PrevisaoFefo(quantidade);
    }

    // Regras comuns de entrada e saida: o servico recusa sozinho o que a tela deixaria passar, sem alterar nada.
    private static void ValidarMovimentacao(Produto produto, int quantidade)
    {
        if (!produto.Ativo)
            throw new InvalidOperationException($"O produto '{produto.Nome}' está inativo e não aceita movimentações.");
        if (quantidade <= 0)
            throw new InvalidOperationException("A quantidade deve ser maior que zero.");
    }

    // Corrigir e cancelar desfazem o efeito da movimentacao no estoque; a movimentacao fica no historico com o motivo.
    public void Corrigir(Guid movimentacaoId, string motivo) =>
        Desfazer(movimentacaoId, motivo, StatusMovimentacao.Corrigida);

    public void Cancelar(Guid movimentacaoId, string motivo) =>
        Desfazer(movimentacaoId, motivo, StatusMovimentacao.Cancelada);

    private void Desfazer(Guid movimentacaoId, string motivo, StatusMovimentacao novoStatus)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Informe o motivo antes de continuar.");

        var movimentacao = dataStore.Movimentacoes.FirstOrDefault(m => m.Id == movimentacaoId)
            ?? throw new InvalidOperationException("Movimentação não encontrada.");

        if (movimentacao.Status != StatusMovimentacao.Ativa)
            throw new InvalidOperationException("Somente movimentações ativas podem ser corrigidas ou canceladas.");

        ReverterEfeitoNoEstoque(movimentacao); // pode falhar (estoque da entrada ja usado): nada muda nesse caso

        movimentacao.Status = novoStatus;
        movimentacao.MotivoCorrecaoOuCancelamento = motivo;
    }

    private static void ReverterEfeitoNoEstoque(Movimentacao movimentacao)
    {
        switch (movimentacao)
        {
            case EntradaEstoque entrada:
                entrada.Produto.RemoverDoLote(entrada.DataValidade, entrada.Quantidade);
                break;
            case SaidaEstoque saida:
                saida.Produto.DevolverLotes(saida.LotesConsumidos);
                break;
            case AjusteBalanco ajuste when ajuste.Quantidade > 0:
                ajuste.Produto.RemoverDoLote(null, ajuste.Quantidade);
                break;
            case AjusteBalanco ajuste when ajuste.Quantidade < 0:
                ajuste.Produto.DevolverLotes(ajuste.LotesConsumidos);
                break;
        }
    }

    // Filtros opcionais e combinaveis; o mais recente vem primeiro. dataFim e o limite maximo da data/hora (inclusive).
    public IReadOnlyList<Movimentacao> ObterHistorico(
        Produto? produto = null, DateTime? dataInicio = null, DateTime? dataFim = null,
        TipoMovimentacao? tipo = null, StatusMovimentacao? status = null) =>
        dataStore.Movimentacoes
            .Where(m => produto is null || m.Produto.Id == produto.Id)
            .Where(m => dataInicio is null || m.Data >= dataInicio)
            .Where(m => dataFim is null || m.Data <= dataFim)
            .Where(m => tipo is null || TipoDe(m) == tipo)
            .Where(m => status is null || m.Status == status)
            .OrderByDescending(m => m.Data)
            .ToList();

    private static TipoMovimentacao TipoDe(Movimentacao m) => m switch
    {
        EntradaEstoque => TipoMovimentacao.Entrada,
        SaidaEstoque => TipoMovimentacao.Saida,
        _ => TipoMovimentacao.Ajuste
    };
}
