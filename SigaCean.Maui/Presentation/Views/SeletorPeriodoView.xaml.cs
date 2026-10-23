using SigaCean.Services;

namespace SigaCean.Presentation.Views;

// Escolha do periodo (mensal, trimestral, anual ou personalizado). Quem calcula os periodos e o PeriodoRelatorio;
// aqui so se escolhe e se avisa a tela (PeriodoAlterado) para ela buscar os dados de novo.
public partial class SeletorPeriodoView : VerticalStackLayout
{
    public event EventHandler? PeriodoAlterado;

    public TipoPeriodo Tipo { get; private set; } = TipoPeriodo.Anual;
    public DateTime Inicio { get; private set; }
    public DateTime Fim { get; private set; } // ultimo dia do periodo (a data, sem hora)

    // Fim do periodo ate o ultimo instante do dia final: e o que o RelatorioService espera.
    public DateTime FimInclusivo => Fim.Date.AddDays(1).AddTicks(-1);

    public string Descricao => PeriodoRelatorio.Descrever(Tipo, Inicio, Fim);

    public SeletorPeriodoView()
    {
        InitializeComponent();
    }

    // Escolhe o tipo (o personalizado parte do ultimo periodo mostrado, para o usuario so ajustar as datas).
    public void Escolher(TipoPeriodo tipo)
    {
        if (tipo == TipoPeriodo.Personalizado)
        {
            DataInicioPicker.Date = Inicio == default ? DateTime.Today.AddDays(-30) : Inicio;
            DataFimPicker.Date = Fim == default ? DateTime.Today : Fim;
            if (Inicio == default) (Inicio, Fim) = (DataInicioPicker.Date!.Value, DataFimPicker.Date!.Value);
        }
        else
        {
            (Inicio, Fim) = PeriodoRelatorio.Contendo(tipo, Inicio == default ? DateTime.Today : Inicio); // ao trocar de tipo, fica no periodo que contem o inicio atual
        }

        Tipo = tipo;
        MostrarMensagem(null);
        AtualizarTela();
        PeriodoAlterado?.Invoke(this, EventArgs.Empty);
    }

    private void OnTipoClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is string nome && Enum.TryParse<TipoPeriodo>(nome, out var tipo))
            Escolher(tipo);
    }

    private void OnAnteriorClicked(object? sender, EventArgs e) => Deslocar(-1);
    private void OnProximoClicked(object? sender, EventArgs e) => Deslocar(1);

    private void Deslocar(int passos)
    {
        (Inicio, Fim) = PeriodoRelatorio.Deslocar(Tipo, Inicio, passos);
        AtualizarTela();
        PeriodoAlterado?.Invoke(this, EventArgs.Empty);
    }

    private void OnAplicarClicked(object? sender, EventArgs e)
    {
        var inicio = DataInicioPicker.Date ?? DateTime.Today;
        var fim = DataFimPicker.Date ?? DateTime.Today;
        if (inicio > fim)
        {
            MostrarMensagem("A data inicial não pode ser depois da data final.");
            return;
        }

        MostrarMensagem(null);
        (Inicio, Fim) = (inicio.Date, fim.Date);
        AtualizarTela();
        PeriodoAlterado?.Invoke(this, EventArgs.Empty);
    }

    private void AtualizarTela()
    {
        Marcar(MensalButton, Tipo == TipoPeriodo.Mensal);
        Marcar(TrimestralButton, Tipo == TipoPeriodo.Trimestral);
        Marcar(AnualButton, Tipo == TipoPeriodo.Anual);
        Marcar(PersonalizadoButton, Tipo == TipoPeriodo.Personalizado);

        NavegacaoLayout.IsVisible = Tipo != TipoPeriodo.Personalizado;
        PersonalizadoLayout.IsVisible = Tipo == TipoPeriodo.Personalizado;
        PeriodoLabel.Text = Descricao;
    }

    // O tipo escolhido fica preenchido; os outros so com contorno (estilos Chip e ChipEscolhido).
    private static void Marcar(Button botao, bool selecionado) =>
        botao.Style = (Style)Application.Current!.Resources[selecionado ? "ChipEscolhido" : "Chip"];

    private void MostrarMensagem(string? texto)
    {
        MensagemLabel.Text = texto ?? string.Empty;
        MensagemLabel.IsVisible = !string.IsNullOrEmpty(texto);
    }
}
