namespace SigaCean.Domain;

public class SaidaEstoque : Movimentacao
{
    public required string Destino { get; init; } // ex.: "CEAN; Canil"

    // Quem levou o produto. O CEAN nao identifica isso hoje ("seria util"); nulo apenas no historico importado da planilha.
    public string? RetiradoPor { get; init; }
}
