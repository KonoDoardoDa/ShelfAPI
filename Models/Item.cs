using System;

namespace ShelfApi.Models;

public class Item
{
    public int Id { get; set;}
    public string? Nome { get; set; }
    public int FornecedorId { get; set; }
    public string? Descricao { get; set; }
}