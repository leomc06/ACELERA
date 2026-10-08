namespace SigaCean.Domain;

// Parcela do estoque (de uma certa validade) retirada por uma saida/ajuste; guardada para que um cancelamento
// devolva a quantidade a mesma validade de onde saiu.
public record LoteConsumido(DateOnly? DataValidade, int Quantidade);
