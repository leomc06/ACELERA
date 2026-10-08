namespace SigaCean.Domain;

// Saldo de um produto que tem a mesma data de validade. Nao e "lote" de fornecedor: o CEAN nao controla lote
// e o sistema nao guarda codigo de lote. Existe so para saber o que vence primeiro (FEFO) e o que ja venceu.
public class LoteEstoque
{
    public DateOnly? DataValidade { get; set; }
    public int Quantidade { get; set; }

    public bool EstaVencido(DateOnly hoje) => DataValidade is not null && DataValidade < hoje;

    public bool EstaProximoDoVencimento(DateOnly hoje, int diasAlerta) =>
        DataValidade is not null && !EstaVencido(hoje) &&
        DataValidade.Value.DayNumber - hoje.DayNumber <= diasAlerta;
}
