using SigaCean.Domain;

namespace SigaCean.Presentation;

// Desenho dos graficos. Usa so o Microsoft.Maui.Graphics que ja vem com o MAUI (sem biblioteca extra, funciona offline).
// Aqui so se DESENHA o que a tela entrega: os numeros vem do RelatorioService.

public enum TipoGrafico { Barras, Pizza, Rosca, BarrasHorizontais }

// Uma barra ou fatia: o texto, o valor e a cor.
public record FatiaGrafico(string Rotulo, double Valor, Color Cor);

// Uma linha da legenda ao lado do grafico.
public class LegendaItem
{
    public required string Rotulo { get; init; }
    public required string ValorTexto { get; init; }
    public required string PercentualTexto { get; init; }
    public required Color Cor { get; init; }
}

public static class PaletaDeCategorias
{
    // Uma cor fixa por categoria: o grafico, a legenda e os cartoes usam a mesma, para dar para reconhecer de relance.
    public static Color Cor(CategoriaProduto categoria) => categoria switch
    {
        CategoriaProduto.Medicamento => Color.FromArgb("#087F78"),
        CategoriaProduto.Insumo => Color.FromArgb("#F59B24"),
        CategoriaProduto.Alimentar => Color.FromArgb("#169B62"),
        CategoriaProduto.EquipamentoHospitalar => Color.FromArgb("#4C7BD9"),
        CategoriaProduto.Higiene => Color.FromArgb("#9B6BD1"),
        CategoriaProduto.Microchip => Color.FromArgb("#D95757"),
        _ => Color.FromArgb("#718589")
    };
}

public class GraficoDrawable : IDrawable
{
    private static readonly Color Tinta = Color.FromArgb("#153B43");
    private static readonly Color Cinza = Color.FromArgb("#718589");
    private static readonly Color Linha = Color.FromArgb("#DBE9E8");

    public TipoGrafico Tipo { get; set; } = TipoGrafico.Barras;
    public IReadOnlyList<FatiaGrafico> Fatias { get; set; } = Array.Empty<FatiaGrafico>();

    // Texto do meio da rosca (ex.: "unidades") e mensagem para quando nao ha nada a mostrar.
    public string TextoCentral { get; set; } = string.Empty;
    public string MensagemVazia { get; set; } = "Sem dados para mostrar.";

    public void Draw(ICanvas canvas, RectF area)
    {
        canvas.SaveState();

        var total = Fatias.Sum(f => f.Valor);
        if (Fatias.Count == 0 || total <= 0)
        {
            DesenharMensagem(canvas, area);
        }
        else
        {
            switch (Tipo)
            {
                case TipoGrafico.Barras: DesenharBarras(canvas, area); break;
                case TipoGrafico.BarrasHorizontais: DesenharBarrasHorizontais(canvas, area); break;
                default: DesenharPizza(canvas, area, rosca: Tipo == TipoGrafico.Rosca, total); break;
            }
        }

        canvas.RestoreState();
    }

    private void DesenharMensagem(ICanvas canvas, RectF area)
    {
        canvas.FontColor = Cinza;
        canvas.FontSize = 14;
        canvas.DrawString(MensagemVazia, area.X, area.Y, area.Width, area.Height,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    // ---------- Barras verticais ----------

    private void DesenharBarras(ICanvas canvas, RectF area)
    {
        const float margemTopo = 26, margemBase = 46, margemLateral = 12;
        var grafico = new RectF(area.X + margemLateral, area.Y + margemTopo,
            area.Width - 2 * margemLateral, area.Height - margemTopo - margemBase);
        var maximo = (float)Fatias.Max(f => f.Valor);

        canvas.StrokeColor = Linha;
        canvas.StrokeSize = 1;
        canvas.DrawLine(grafico.Left, grafico.Bottom, grafico.Right, grafico.Bottom);

        var faixa = grafico.Width / Fatias.Count;
        var larguraBarra = Math.Min(faixa * 0.62f, 90f);

        for (var i = 0; i < Fatias.Count; i++)
        {
            var fatia = Fatias[i];
            var altura = maximo <= 0 ? 0 : (float)(fatia.Valor / maximo) * grafico.Height;
            var x = grafico.Left + i * faixa + (faixa - larguraBarra) / 2;
            var y = grafico.Bottom - altura;

            if (altura > 0)
            {
                canvas.FillColor = fatia.Cor;
                canvas.FillRoundedRectangle(x, y, larguraBarra, altura, 5);
            }

            // Valor acima da barra
            canvas.FontColor = Tinta;
            canvas.FontSize = 13;
            canvas.DrawString(TextosDeConsumo.Numero(fatia.Valor), x - 20, y - 22, larguraBarra + 40, 20,
                HorizontalAlignment.Center, VerticalAlignment.Bottom);

            // Nome da categoria abaixo (quebra em ate 2 linhas)
            canvas.FontColor = Cinza;
            canvas.FontSize = 11;
            canvas.DrawString(fatia.Rotulo, grafico.Left + i * faixa + 2, grafico.Bottom + 6, faixa - 4, margemBase - 8,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }
    }

    // ---------- Barras horizontais (produtos dentro de uma categoria) ----------

    private void DesenharBarrasHorizontais(ICanvas canvas, RectF area)
    {
        var larguraRotulo = Math.Min(area.Width * 0.42f, 300f);
        const float margemDireita = 56;
        var maximo = (float)Fatias.Max(f => f.Valor);
        var linhaAltura = area.Height / Fatias.Count;
        var alturaBarra = Math.Min(linhaAltura * 0.6f, 22f);
        var larguraMaxima = area.Width - larguraRotulo - margemDireita - 8;

        for (var i = 0; i < Fatias.Count; i++)
        {
            var fatia = Fatias[i];
            var y = area.Y + i * linhaAltura + (linhaAltura - alturaBarra) / 2;
            var largura = maximo <= 0 ? 0 : (float)(fatia.Valor / maximo) * larguraMaxima;

            canvas.FontColor = Tinta;
            canvas.FontSize = 12;
            canvas.DrawString(TextosDeConsumo.Encurtar(fatia.Rotulo, 44), area.X, y - 4, larguraRotulo - 8, alturaBarra + 8,
                HorizontalAlignment.Right, VerticalAlignment.Center);

            canvas.FillColor = fatia.Cor;
            canvas.FillRoundedRectangle(area.X + larguraRotulo, y, Math.Max(largura, 3f), alturaBarra, 4);

            canvas.FontColor = Tinta;
            canvas.FontSize = 12;
            canvas.DrawString(TextosDeConsumo.Numero(fatia.Valor), area.X + larguraRotulo + Math.Max(largura, 3f) + 6, y - 4,
                margemDireita, alturaBarra + 8, HorizontalAlignment.Left, VerticalAlignment.Center);
        }
    }

    // ---------- Pizza e rosca ----------

    private void DesenharPizza(ICanvas canvas, RectF area, bool rosca, double total)
    {
        var diametro = Math.Min(area.Width, area.Height) - 16;
        var raio = diametro / 2f;
        var centro = new PointF(area.X + area.Width / 2, area.Y + area.Height / 2);

        // Comeca no topo e anda no sentido horario. Cada fatia e um poligono (centro + pontos do arco):
        // assim o desenho nao depende de como cada plataforma interpreta os angulos.
        var inicio = -Math.PI / 2;
        foreach (var fatia in Fatias.Where(f => f.Valor > 0))
        {
            var varredura = fatia.Valor / total * 2 * Math.PI;

            var caminho = new PathF();
            caminho.MoveTo(centro);
            var passos = Math.Max(2, (int)Math.Ceiling(varredura / (Math.PI / 90)));
            for (var p = 0; p <= passos; p++)
            {
                var angulo = inicio + varredura * p / passos;
                caminho.LineTo(centro.X + raio * (float)Math.Cos(angulo), centro.Y + raio * (float)Math.Sin(angulo));
            }
            caminho.Close();

            canvas.FillColor = fatia.Cor;
            canvas.FillPath(caminho);
            canvas.StrokeColor = Colors.White;
            canvas.StrokeSize = 2;
            canvas.DrawPath(caminho);

            // Percentual dentro da fatia (so quando cabe)
            var percentual = fatia.Valor / total;
            if (percentual >= 0.07)
            {
                var meio = inicio + varredura / 2;
                var distancia = rosca ? raio * 0.78f : raio * 0.62f;
                var x = centro.X + distancia * (float)Math.Cos(meio);
                var y = centro.Y + distancia * (float)Math.Sin(meio);
                canvas.FontColor = Colors.White;
                canvas.FontSize = 13;
                canvas.DrawString(TextosDeConsumo.Percentual(fatia.Valor, total), x - 30, y - 10, 60, 20,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }

            inicio += varredura;
        }

        if (rosca)
        {
            // Furo do meio, com o total escrito.
            canvas.FillColor = Colors.White;
            canvas.FillCircle(centro, raio * 0.52f);

            canvas.FontColor = Tinta;
            canvas.FontSize = 22;
            canvas.DrawString(TextosDeConsumo.Numero(total), centro.X - 60, centro.Y - 20, 120, 24,
                HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.FontColor = Cinza;
            canvas.FontSize = 11;
            canvas.DrawString(TextoCentral, centro.X - 60, centro.Y + 4, 120, 20,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }
    }
}
