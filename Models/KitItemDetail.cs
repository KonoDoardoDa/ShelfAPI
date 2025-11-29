using System;

namespace ShelfApi.Models;

public class KitItemDetail
{
    public int ItemId { get; set; }
    public string? ItemNome { get; set; }
    public int Quantidade { get; set; }
    public bool IsPainel { get; set; }
}