using SigaCean.Domain;
using SigaCean.Services;

namespace SigaCean.Presentation.ViewModels;

// Uma linha da ficha do produto: quanto ha em estoque com cada validade, na ordem em que sairia (FEFO).
// So formata; quem decide se esta vencido ou proximo do vencimento e o proprio LoteEstoque.
public class SaldoValidadeItem
{
    public required LoteEstoque Saldo { get; init; }
    public required int Ordem { get; init; } // 1 = o primeiro a sair
    public DateOnly Hoje { get; init; } = DateOnly.FromDateTime(DateTime.Today);

    public string OrdemTexto => $"{Ordem}º";
    public string QuantidadeTexto => Saldo.Quantidade.ToString();

    public string ValidadeTexto => Saldo.DataValidade is { } data ? data.ToString("dd/MM/yyyy") : "Sem validade informada";

    public string SituacaoTexto => TextosDeEstoque.DescreverSaldo(Saldo.DataValidade, Hoje);

    public Color CorSituacao =>
        Saldo.EstaVencido(Hoje) ? Color.FromArgb("#D95757")
        : Saldo.EstaProximoDoVencimento(Hoje, EstoqueService.DiasAlertaValidade) ? Color.FromArgb("#F59B24")
        : Color.FromArgb("#718589");
}
