using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using SigaCean.Services;
// O MAUI tem tipos com os mesmos nomes; aqui valem os do MigraDoc.
using Color = MigraDoc.DocumentObjectModel.Color;
using Colors = MigraDoc.DocumentObjectModel.Colors;
using VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment;

namespace SigaCean.Presentation.Reports;

// Monta o PDF do "Relatorio por categoria": o mesmo conteudo da tela (uso por categoria e, dentro de cada uma,
// o produto mais e o menos usado). Os numeros vem do RelatorioService; aqui so se escreve o documento.
public static class RelatorioCategoriaPdf
{
    private const int ProdutosPorCategoria = 5; // quantos produtos de cada categoria entram na tabela do PDF

    private static readonly Color Verde = Color.FromRgb(0x07, 0x5F, 0x5B); // cor principal do sistema
    private static readonly Color Cinza = Color.FromRgb(0x5F, 0x6B, 0x6A);
    private static readonly Color Linha = Color.FromRgb(0xD9, 0xE2, 0xE1);

    public static void Gerar(
        string caminho,
        IReadOnlyList<ConsumoPorCategoria> categorias,
        TipoPeriodo tipo,
        DateTime dataInicio,
        DateTime dataFim,
        string? usuario)
    {
        // Usa as fontes instaladas no Windows (Arial), sem precisar embutir arquivos de fonte.
        GlobalFontSettings.UseWindowsFontsUnderWindows = true;

        var documento = new Document();
        documento.Info.Title = "Relatório por Categoria - CEAN";
        DefinirEstilos(documento);

        var secao = documento.AddSection();
        secao.PageSetup.PageFormat = PageFormat.A4;
        secao.PageSetup.LeftMargin = Unit.FromCentimeter(1.8);
        secao.PageSetup.RightMargin = Unit.FromCentimeter(1.8);
        secao.PageSetup.TopMargin = Unit.FromCentimeter(1.8);
        secao.PageSetup.BottomMargin = Unit.FromCentimeter(2);

        MontarCabecalho(secao, tipo, dataInicio, dataFim, usuario);
        MontarResumo(secao, categorias);
        MontarTabelaGeral(secao, categorias);
        MontarDetalhePorCategoria(secao, categorias);
        MontarRodape(secao);

        var renderer = new PdfDocumentRenderer { Document = documento };
        renderer.RenderDocument();
        renderer.PdfDocument.Save(caminho);
    }

    public static string NomeSugerido(TipoPeriodo tipo, DateTime dataInicio, DateTime dataFim)
    {
        var periodo = tipo switch
        {
            TipoPeriodo.Mensal => $"Mensal_{dataInicio:yyyy-MM}",
            TipoPeriodo.Trimestral => $"Trimestral_{dataInicio:yyyy}-T{(dataInicio.Month - 1) / 3 + 1}",
            TipoPeriodo.Anual => $"Anual_{dataInicio:yyyy}",
            _ => $"{dataInicio:yyyyMMdd}_a_{dataFim:yyyyMMdd}"
        };
        return $"Relatorio_Por_Categoria_CEAN_{periodo}.pdf";
    }

    private static void DefinirEstilos(Document documento)
    {
        var normal = documento.Styles[StyleNames.Normal]!;
        normal.Font.Name = "Arial";
        normal.Font.Size = 10;

        var rodape = documento.Styles[StyleNames.Footer]!;
        rodape.Font.Size = 8;
        rodape.Font.Color = Cinza;
    }

    private static void MontarCabecalho(Section secao, TipoPeriodo tipo, DateTime dataInicio, DateTime dataFim, string? usuario)
    {
        var eyebrow = secao.AddParagraph("CEAN • CENTRO DE APOIO AOS ANIMAIS");
        eyebrow.Format.Font.Size = 8;
        eyebrow.Format.Font.Bold = true;
        eyebrow.Format.Font.Color = Cinza;

        var titulo = secao.AddParagraph("Relatório por Categoria");
        titulo.Format.Font.Size = 20;
        titulo.Format.Font.Bold = true;
        titulo.Format.Font.Color = Verde;
        titulo.Format.SpaceBefore = 2;

        var subtitulo = secao.AddParagraph("Uso do estoque por categoria e produtos mais e menos usados");
        subtitulo.Format.Font.Color = Cinza;

        var periodo = secao.AddParagraph($"Período: {PeriodoRelatorio.Descrever(tipo, dataInicio, dataFim)}  ({dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy})");
        periodo.Format.SpaceBefore = 6;

        var nota = secao.AddParagraph("Uso = saídas registradas no período. Entradas e ajustes de balanço não contam. O estoque é o de hoje.");
        nota.Format.Font.Size = 9;
        nota.Format.Font.Color = Cinza;

        var emissao = secao.AddParagraph(
            $"Emitido em {DateTime.Now:dd/MM/yyyy 'às' HH:mm}" + (string.IsNullOrWhiteSpace(usuario) ? string.Empty : $" por {usuario}"));
        emissao.Format.Font.Size = 9;
        emissao.Format.Font.Color = Cinza;
        emissao.Format.SpaceAfter = 12;
    }

    private static void MontarResumo(Section secao, IReadOnlyList<ConsumoPorCategoria> categorias)
    {
        var totalUsado = categorias.Sum(c => c.TotalSaidas);
        var maisUsada = categorias.Where(c => c.TotalSaidas > 0).OrderByDescending(c => c.TotalSaidas).FirstOrDefault();

        var texto = totalUsado == 0
            ? "Nenhuma saída registrada neste período."
            : $"{TextosDeConsumo.Numero(totalUsado)} unidade(s) usadas no período  •  Categoria mais usada: {Rotulos.Categoria(maisUsada!.Categoria)}";

        var resumo = secao.AddParagraph(texto);
        resumo.Format.Font.Bold = true;
        resumo.Format.SpaceAfter = 10;
    }

    // Uma linha por categoria: visao geral para comparar.
    private static void MontarTabelaGeral(Section secao, IReadOnlyList<ConsumoPorCategoria> categorias)
    {
        var totalUsado = categorias.Sum(c => c.TotalSaidas);

        var tabela = secao.AddTable();
        tabela.Borders.Color = Linha;
        tabela.Borders.Bottom.Width = 0.5;
        tabela.Rows.LeftIndent = 0;

        tabela.AddColumn(Unit.FromCentimeter(5.2));
        foreach (var largura in new[] { 2.4, 3.0, 3.0, 3.6 })
            tabela.AddColumn(Unit.FromCentimeter(largura));

        var cabecalho = tabela.AddRow();
        cabecalho.HeadingFormat = true;
        cabecalho.Shading.Color = Verde;
        cabecalho.Format.Font.Color = Colors.White;
        cabecalho.Format.Font.Bold = true;
        cabecalho.TopPadding = 4;
        cabecalho.BottomPadding = 4;
        Celula(cabecalho, 0, "Categoria", ParagraphAlignment.Left);
        Celula(cabecalho, 1, "Produtos", ParagraphAlignment.Right);
        Celula(cabecalho, 2, "Em estoque", ParagraphAlignment.Right);
        Celula(cabecalho, 3, "Usado no período", ParagraphAlignment.Right);
        Celula(cabecalho, 4, "% do uso total", ParagraphAlignment.Right);

        foreach (var c in categorias.OrderByDescending(c => c.TotalSaidas).ThenBy(c => Rotulos.Categoria(c.Categoria)))
        {
            var linha = tabela.AddRow();
            linha.TopPadding = 3;
            linha.BottomPadding = 3;
            Celula(linha, 0, Rotulos.Categoria(c.Categoria), ParagraphAlignment.Left);
            Celula(linha, 1, c.QuantidadeDeProdutos.ToString(), ParagraphAlignment.Right);
            Celula(linha, 2, TextosDeConsumo.Numero(c.EstoqueAtual), ParagraphAlignment.Right);
            Celula(linha, 3, TextosDeConsumo.Numero(c.TotalSaidas), ParagraphAlignment.Right);
            Celula(linha, 4, TextosDeConsumo.Percentual(c.TotalSaidas, totalUsado), ParagraphAlignment.Right);
        }
    }

    // Para cada categoria com uso: quem foi o mais e o menos usado e os produtos que mais sairam.
    private static void MontarDetalhePorCategoria(Section secao, IReadOnlyList<ConsumoPorCategoria> categorias)
    {
        foreach (var c in categorias.Where(c => c.QuantidadeDeProdutos > 0).OrderByDescending(c => c.TotalSaidas).ThenBy(c => Rotulos.Categoria(c.Categoria)))
        {
            var titulo = secao.AddParagraph(Rotulos.Categoria(c.Categoria));
            titulo.Format.Font.Size = 13;
            titulo.Format.Font.Bold = true;
            titulo.Format.Font.Color = Verde;
            titulo.Format.SpaceBefore = 16;
            titulo.Format.KeepWithNext = true;

            AdicionarLinha(secao, TextosDeConsumo.Resumo(c), cinza: true);
            AdicionarLinha(secao, TextosDeConsumo.MaisUsado(c), negrito: c.MaisUsado is not null);
            if (c.MenosUsado is not null) AdicionarLinha(secao, TextosDeConsumo.MenosUsado(c), negrito: true);
            if (TextosDeConsumo.SemSaida(c).Length > 0) AdicionarLinha(secao, TextosDeConsumo.SemSaida(c), cinza: true);

            if (c.ProdutosUsados.Count == 0) continue;

            var tabela = secao.AddTable();
            tabela.Borders.Color = Linha;
            tabela.Borders.Bottom.Width = 0.5;
            tabela.Rows.LeftIndent = 0;
            tabela.AddColumn(Unit.FromCentimeter(13.6));
            tabela.AddColumn(Unit.FromCentimeter(3.6));

            var cabecalho = tabela.AddRow();
            cabecalho.HeadingFormat = true;
            cabecalho.Shading.Color = Verde;
            cabecalho.Format.Font.Color = Colors.White;
            cabecalho.Format.Font.Bold = true;
            cabecalho.TopPadding = 3;
            cabecalho.BottomPadding = 3;
            Celula(cabecalho, 0, c.ProdutosUsados.Count > ProdutosPorCategoria ? $"Os {ProdutosPorCategoria} mais usados" : "Produtos usados", ParagraphAlignment.Left);
            Celula(cabecalho, 1, "Saídas", ParagraphAlignment.Right);

            foreach (var item in c.ProdutosUsados.Take(ProdutosPorCategoria))
            {
                var linha = tabela.AddRow();
                linha.TopPadding = 2;
                linha.BottomPadding = 2;
                Celula(linha, 0, item.Produto.Nome, ParagraphAlignment.Left);
                Celula(linha, 1, TextosDeConsumo.Numero(item.TotalSaidas), ParagraphAlignment.Right);
            }

            cabecalho.KeepWith = tabela.Rows.Count - 1; // o cabecalho fica junto das linhas (a tabela nao quebra no meio)
        }
    }

    private static void AdicionarLinha(Section secao, string texto, bool cinza = false, bool negrito = false)
    {
        var paragrafo = secao.AddParagraph(texto);
        paragrafo.Format.SpaceBefore = 2;
        paragrafo.Format.KeepWithNext = true; // a categoria nao se separa da tabela que vem depois
        paragrafo.Format.Font.Bold = negrito;
        if (cinza) paragrafo.Format.Font.Color = Cinza;
    }

    private static void MontarRodape(Section secao)
    {
        var rodape = secao.Footers.Primary.AddParagraph();
        rodape.Format.Alignment = ParagraphAlignment.Center;
        rodape.AddText("SIGA-CEAN  •  Página ");
        rodape.AddPageField();
        rodape.AddText(" de ");
        rodape.AddNumPagesField();
    }

    private static void Celula(Row linha, int coluna, string texto, ParagraphAlignment alinhamento)
    {
        var celula = linha.Cells[coluna];
        celula.VerticalAlignment = VerticalAlignment.Center;
        var paragrafo = celula.AddParagraph(texto);
        paragrafo.Format.Alignment = alinhamento;
    }
}
