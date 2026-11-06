namespace SigaCean.Domain;

public class EntradaEstoque : Movimentacao
{
    // Nulo apenas no historico importado da planilha do CEAN, que nem sempre informa a origem.
    // Entradas novas registradas pelo sistema sempre informam (RD0006).
    public TipoOrigemEntrada? TipoOrigem { get; init; }
    public string? IdentificacaoOrigem { get; init; } // numero ou codigo que identifica a origem (ex.: a AF de uma compra/licitacao)
    public string? Documento { get; init; } // nota fiscal, pedido de compra ou documento de transferencia/doacao
    public string? DetalheOrigem { get; init; } // ex.: nome do doador, "Santa Casa", etc.
    public DateOnly? DataValidade { get; init; }
}
