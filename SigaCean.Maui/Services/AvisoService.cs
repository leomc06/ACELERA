namespace SigaCean.Services;

// Recado de "deu certo" que uma tela deixa para a tela seguinte mostrar (ex.: apos registrar uma saida, a tela de
// Estoque exibe a confirmacao). Fica so em memoria e e lido uma unica vez.
public class AvisoService
{
    private string? _pendente;

    public void Publicar(string mensagem) => _pendente = mensagem;

    // Devolve o recado pendente e o apaga, para nao aparecer de novo ao voltar para a tela outras vezes.
    public string? Consumir()
    {
        var mensagem = _pendente;
        _pendente = null;
        return mensagem;
    }
}
