namespace SigaCean.Domain;

// O CEAN nao controla lote hoje, mas o time decidiu implementar: cada entrada pode abrir um lote
// proprio com sua propria validade, ja que produtos diferentes do mesmo medicamento podem vencer em datas diferentes.
public class LoteEstoque
{
    public string? Codigo { get; set; }
    public DateOnly? DataValidade { get; set; }
    public int Quantidade { get; set; }

    public bool EstaVencido(DateOnly hoje) => DataValidade is not null && DataValidade < hoje;

    public bool EstaProximoDoVencimento(DateOnly hoje, int diasAlerta) =>
        DataValidade is not null && !EstaVencido(hoje) &&
        DataValidade.Value.DayNumber - hoje.DayNumber <= diasAlerta;
}
