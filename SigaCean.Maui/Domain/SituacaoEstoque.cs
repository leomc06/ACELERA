namespace SigaCean.Domain;

// Situacoes de alerta usadas para filtrar a tela de Estoque.
public enum SituacaoEstoque
{
    Todos,
    SemEstoque,
    Critico,
    AbaixoDoMinimo,
    ValidadeProxima,
    Vencido
}
