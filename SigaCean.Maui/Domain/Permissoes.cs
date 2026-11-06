namespace SigaCean.Domain;

// O que cada perfil pode fazer. Os dois perfis registram saida; entrada, cadastro, ajuste, edicao, correcao,
// cancelamento e relatorios continuam so do Administrativo.
public static class Permissoes
{
    public static bool PodeRegistrarSaida(Usuario? usuario) => usuario is not null;

    public static bool PodeAdministrar(Usuario? usuario) => usuario?.Perfil == Perfil.Administrativo;
}
