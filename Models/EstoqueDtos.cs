using System;

namespace ShelfApi.Models;

public class Estoque
{
    public int ItemId { get; set; }
    public int Quantidade { get; set; }
}

public class MovimentacaoEstoqueDto
{
    public int ItemId { get; set; }
    public string Tipo { get; set; } = "Entrada"; // "Entrada" ou "Saida"
    public int Quantidade { get; set; }
    public string? Motivo { get; set; }
    public int? UsuarioId { get; set; }
    public string? UsuarioNome { get; set; }
}

public class EstoqueSaldoDto
{
    public int ItemId { get; set; }
    public string? NomeItem { get; set; }
    public int Quantidade { get; set; }
}
