using SigaCean.Domain;

namespace SigaCean.Presentation;

// Textos de exibicao dos enums, definidos uma unica vez para todas as telas.
public static class Rotulos
{
    // Ordem em que as categorias aparecem nas listas de escolha.
    public static readonly CategoriaProduto[] Categorias =
    [
        CategoriaProduto.Medicamento,
        CategoriaProduto.Insumo,
        CategoriaProduto.Alimentar,
        CategoriaProduto.EquipamentoHospitalar,
        CategoriaProduto.Higiene,
        CategoriaProduto.Microchip
    ];

    // "Todas as categorias" + cada categoria, para os filtros (Valor nulo = sem filtro).
    public static readonly (CategoriaProduto? Valor, string Texto)[] CategoriasParaFiltro =
        [(null, "Todas as categorias"), .. Categorias.Select(c => ((CategoriaProduto?)c, Categoria(c)))];

    public static string Categoria(CategoriaProduto categoria) => categoria switch
    {
        CategoriaProduto.Alimentar => "Alimentar",
        CategoriaProduto.Insumo => "Insumo",
        CategoriaProduto.EquipamentoHospitalar => "Equipamento hospitalar",
        CategoriaProduto.Medicamento => "Medicamento",
        CategoriaProduto.Higiene => "Higiene",
        CategoriaProduto.Microchip => "Microchip",
        _ => categoria.ToString()
    };

    public static string Perfil(Domain.Perfil? perfil) => perfil switch
    {
        Domain.Perfil.Administrativo => "Administrativo",
        Domain.Perfil.Veterinario => "Veterinário",
        _ => string.Empty
    };
}
