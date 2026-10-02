namespace SigaCean.Domain;

public abstract class Movimentacao
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Produto Produto { get; init; } // sempre uma referencia a um produto ja cadastrado, nunca texto livre
    public required int Quantidade { get; init; }
    public DateTime Data { get; init; } = DateTime.Now;
    public StatusMovimentacao Status { get; set; } = StatusMovimentacao.Ativa;
    public Usuario? UsuarioResponsavel { get; set; } // opcional: o CEAN nao registra isso hoje, mas "seria util"
    public string? MotivoCorrecaoOuCancelamento { get; set; }
    public List<LoteConsumido> LotesConsumidos { get; } = new(); // preenchido por saidas e ajustes para baixo
}
