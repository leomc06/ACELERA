namespace SigaCean.Domain;

public class Usuario
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Nome { get; set; }
    public required string LoginUsuario { get; set; }
    public required string Senha { get; set; } // texto puro: protótipo acadêmico sem persistência, sem exposição externa
    public required Perfil Perfil { get; set; }

    public bool ValidarSenha(string senhaInformada) => Senha == senhaInformada;
}
