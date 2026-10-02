using SigaCean.Services;

namespace SigaCean.Presentation.ViewModels;

public class RelatorioConsumoItem
{
    public required ItemRelatorioConsumo Item { get; init; }

    public string ProdutoNome => Item.Produto.Nome;
    public string EntradasTexto => Item.TotalEntradas.ToString();
    public string SaidasTexto => Item.TotalSaidas.ToString();

    public string MediaMensalTexto => Item.MediaMensalSaidas.ToString("0.#");
    public string EstoqueAtualTexto => Item.EstoqueAtual.ToString();

    public string AjustesTexto => Item.TotalAjustes switch
    {
        0 => "0",
        > 0 => $"+{Item.TotalAjustes}",
        _ => Item.TotalAjustes.ToString()
    };
}
