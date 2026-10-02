using SigaCean.Domain;
using SigaCean.Infrastructure;

namespace SigaCean.Services;

public class UsuarioService(DataStore dataStore)
{
    public Usuario? UsuarioLogado { get; private set; }

    public Usuario? Login(string loginUsuario, string senha)
    {
        var usuario = dataStore.Usuarios.FirstOrDefault(u =>
            string.Equals(u.LoginUsuario, loginUsuario, StringComparison.OrdinalIgnoreCase));

        if (usuario is null || !usuario.ValidarSenha(senha))
            return null;

        UsuarioLogado = usuario;
        return usuario;
    }

    public void Logout() => UsuarioLogado = null;
}
