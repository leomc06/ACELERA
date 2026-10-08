using SigaCean.Domain;

namespace SigaCean.Presentation.Views;

// Topo comum das telas principais: nome da instituicao e quem esta logado.
public partial class CabecalhoView : Grid
{
    public CabecalhoView()
    {
        InitializeComponent();
    }

    public void Atualizar(Usuario? usuario)
    {
        UsuarioInicialLabel.Text = usuario is null ? "?" : usuario.Nome[..1].ToUpperInvariant();
        UsuarioNomeLabel.Text = usuario?.Nome ?? string.Empty;
        UsuarioPerfilLabel.Text = Rotulos.Perfil(usuario?.Perfil);
    }
}
