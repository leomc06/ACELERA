namespace SigaCean.Presentation.Views;

// Faixa verde de confirmacao ("Saida registrada..."): aparece no topo da tela e some sozinha.
public partial class AvisoBanner : Border
{
    private static readonly TimeSpan Duracao = TimeSpan.FromSeconds(8);
    private int _exibicao;

    public AvisoBanner()
    {
        InitializeComponent();
    }

    // Sem mensagem, nao mostra nada.
    public void Mostrar(string? mensagem)
    {
        if (string.IsNullOrWhiteSpace(mensagem))
        {
            IsVisible = false;
            return;
        }

        MensagemLabel.Text = mensagem;
        IsVisible = true;

        // Se outra mensagem aparecer antes do prazo, so a ultima esconde a faixa.
        var atual = ++_exibicao;
        Dispatcher.DispatchDelayed(Duracao, () =>
        {
            if (atual == _exibicao) IsVisible = false;
        });
    }

    private void OnFecharClicked(object? sender, EventArgs e)
    {
        _exibicao++;
        IsVisible = false;
    }
}
