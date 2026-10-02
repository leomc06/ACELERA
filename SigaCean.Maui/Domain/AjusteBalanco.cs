namespace SigaCean.Domain;

// Corresponde ao valor "BALANCO" observado na planilha real do CEAN: uma correcao apos
// conferencia fisica trimestral, distinta de uma entrada/saida de uso normal (RD0023).
public class AjusteBalanco : Movimentacao
{
    public required int QuantidadeAnterior { get; init; }
    public required int QuantidadeApurada { get; init; }
}
