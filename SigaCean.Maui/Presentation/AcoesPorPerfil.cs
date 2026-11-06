using SigaCean.Domain;

namespace SigaCean.Presentation;

// Mostra em cada tela so os botoes de acao que o perfil pode usar (regras em Domain/Permissoes).
public static class AcoesPorPerfil
{
    public static void Aplicar(Layout acoes, Button saida, Usuario? usuario)
    {
        foreach (var botao in acoes.Children.OfType<Button>())
            botao.IsVisible = botao == saida ? Permissoes.PodeRegistrarSaida(usuario) : Permissoes.PodeAdministrar(usuario);
        acoes.IsVisible = acoes.Children.OfType<Button>().Any(b => b.IsVisible);
    }
}
