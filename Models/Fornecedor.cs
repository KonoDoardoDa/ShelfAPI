using System;

namespace ShelfApi.Models;

public class Fornecedor
{
    public int Id { get; set; }
    public string? Nome { get; set; }
    public bool Ativo { get; set; } = true;
}
