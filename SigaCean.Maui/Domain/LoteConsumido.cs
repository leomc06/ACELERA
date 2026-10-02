namespace SigaCean.Domain;

// Parcela de um lote retirada por uma saida/ajuste; guardada para que um cancelamento devolva ao lote de origem.
public record LoteConsumido(string? Codigo, DateOnly? DataValidade, int Quantidade);
