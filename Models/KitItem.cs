using System;
using System.ComponentModel.DataAnnotations;

namespace ShelfApi.Models;

public class KitItem
{
    [Required]
    public int KitId { get; set; }

    [Required]
    public int ItemId { get; set; }

    [Range(1, 1_000_000, ErrorMessage = "A quantidade do item no kit deve ser entre 1 e 1.000.000 unidades.")]
    public int Quantidade { get; set; }

    public bool IsPainel { get; set; }
}

