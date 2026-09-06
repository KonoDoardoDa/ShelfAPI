using System;

namespace ShelfApi.Models;

public class MovimentacaoEstoque
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string Tipo { get; set; } = "Entrada";
    public int Quantidade { get; set; }
    public int SaldoAnterior { get; set; }
    public int SaldoPosterior { get; set; }
    public string? Motivo { get; set; }
    public int? UsuarioId { get; set; }
    public string UsuarioNome { get; set; } = "Sistema";
    public DateTime DataMovimentacao { get; set; } = DateTime.UtcNow;
}

public class MovimentacaoEstoqueDetalheDto
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string NomeItem { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Entrada";
    public int Quantidade { get; set; }
    public int SaldoAnterior { get; set; }
    public int SaldoPosterior { get; set; }
    public string? Motivo { get; set; }
    public int? UsuarioId { get; set; }
    public string UsuarioNome { get; set; } = "Sistema";
    public DateTime DataMovimentacao { get; set; }
}
