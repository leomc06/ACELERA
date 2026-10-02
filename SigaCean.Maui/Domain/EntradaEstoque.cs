namespace SigaCean.Domain;

public class EntradaEstoque : Movimentacao
{
    // Nulo apenas no historico importado da planilha do CEAN, que nem sempre informa a origem.
    // Entradas novas registradas pelo sistema sempre informam (RD0006).
    public TipoOrigemEntrada? TipoOrigem { get; init; }
    public string? NumeroAF { get; init; } // so faz sentido para compra/licitacao
    public string? Documento { get; init; } // nota fiscal, pedido de compra ou documento de transferencia/doacao
    public string? DetalheOrigem { get; init; } // ex.: nome do doador, "Santa Casa", etc.
    public string? Lote { get; init; }
    public DateOnly? DataValidade { get; init; }
}
