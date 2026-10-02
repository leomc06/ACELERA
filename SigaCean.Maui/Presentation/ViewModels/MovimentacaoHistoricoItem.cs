using SigaCean.Domain;

namespace SigaCean.Presentation.ViewModels;

public class MovimentacaoHistoricoItem
{
    public required Movimentacao Movimentacao { get; init; }

    public string DataFormatada => Movimentacao.Data.ToString("dd/MM/yyyy HH:mm");
    public string ProdutoNome => Movimentacao.Produto.Nome;

    public string TipoTexto => Movimentacao switch
    {
        EntradaEstoque => "Entrada",
        SaidaEstoque => "Saída",
        AjusteBalanco => "Ajuste de balanço",
        _ => "Movimentação"
    };

    public Color CorTipo => Movimentacao switch
    {
        EntradaEstoque => Color.FromArgb("#169B62"),
        SaidaEstoque => Color.FromArgb("#D95757"),
        AjusteBalanco => Color.FromArgb("#F59B24"),
        _ => Colors.Gray
    };

    public string QuantidadeTexto => Movimentacao switch
    {
        EntradaEstoque e => $"+{e.Quantidade}",
        SaidaEstoque s => $"-{s.Quantidade}",
        AjusteBalanco a => a.Quantidade >= 0 ? $"+{a.Quantidade}" : a.Quantidade.ToString(),
        _ => Movimentacao.Quantidade.ToString()
    };

    public string DetalheTexto => Movimentacao switch
    {
        EntradaEstoque e => MontarDetalheEntrada(e),
        SaidaEstoque s => string.IsNullOrWhiteSpace(s.RetiradoPor)
            ? $"Destino: {s.Destino}"
            : $"Destino: {s.Destino} · Retirado por: {s.RetiradoPor}",
        AjusteBalanco a => $"Balanço: {a.QuantidadeAnterior} → {a.QuantidadeApurada}",
        _ => string.Empty
    };

    public string StatusTexto => Movimentacao.Status switch
    {
        StatusMovimentacao.Ativa => "Ativa",
        StatusMovimentacao.Corrigida => "Corrigida",
        StatusMovimentacao.Cancelada => "Cancelada",
        _ => Movimentacao.Status.ToString()
    };

    public bool MostrarStatusEspecial => Movimentacao.Status != StatusMovimentacao.Ativa;

    public string UsuarioTexto => Movimentacao.UsuarioResponsavel?.Nome ?? "—";

    private static string MontarDetalheEntrada(EntradaEstoque e)
    {
        var tipo = e.TipoOrigem switch
        {
            TipoOrigemEntrada.Compra => "Compra",
            TipoOrigemEntrada.Concessao => "Concessão",
            TipoOrigemEntrada.Doacao => "Doação",
            null => "Origem não informada",
            _ => e.TipoOrigem.ToString()!
        };

        var partes = new List<string> { tipo };
        if (!string.IsNullOrWhiteSpace(e.NumeroAF)) partes.Add($"AF {e.NumeroAF}");
        if (!string.IsNullOrWhiteSpace(e.Documento)) partes.Add($"doc. {e.Documento}");
        if (!string.IsNullOrWhiteSpace(e.DetalheOrigem)) partes.Add(e.DetalheOrigem);
        if (!string.IsNullOrWhiteSpace(e.Lote)) partes.Add($"lote {e.Lote}");
        if (e.DataValidade is not null) partes.Add($"val. {e.DataValidade:dd/MM/yyyy}");

        return string.Join(" · ", partes);
    }
}
