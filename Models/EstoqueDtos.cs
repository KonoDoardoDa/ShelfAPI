using System;
using System.ComponentModel.DataAnnotations;

namespace ShelfApi.Models;

public class Estoque
{
    public int ItemId { get; set; }
    public int Quantidade { get; set; }
}

public class MovimentacaoEstoqueDto
{
    [Required(ErrorMessage = "O ID do item é obrigatório.")]
    [Range(1, int.MaxValue, ErrorMessage = "O ID do item deve ser válido.")]
    public int ItemId { get; set; }

    [Required(ErrorMessage = "O tipo de movimentação é obrigatório.")]
    public string Tipo { get; set; } = "Entrada"; // "Entrada" ou "Saida"

    [Required(ErrorMessage = "A quantidade é obrigatória.")]
    [Range(1, 1_000_000, ErrorMessage = "A quantidade movimentada deve ser entre 1 e 1.000.000 unidades.")]
    public int Quantidade { get; set; }

    [StringLength(255, ErrorMessage = "O motivo não pode exceder 255 caracteres.")]
    public string? Motivo { get; set; }

    [StringLength(100, ErrorMessage = "A Nota Fiscal não pode exceder 100 caracteres.")]
    public string? NotaFiscal { get; set; }

    public int? UsuarioId { get; set; }
    public string? UsuarioNome { get; set; }
}

public class EstoqueSaldoDto
{
    public int ItemId { get; set; }
    public string? NomeItem { get; set; }
    public int Quantidade { get; set; }
}
