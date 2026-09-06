using System;

namespace ShelfApi.Models;

public class Software
{
    public int Id { get; set; }
    public string NomeSoftware { get; set; } = string.Empty;
    public string Versao { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public int? KitId { get; set; }
    public string? NomeKit { get; set; }
    public string TipoDispositivo { get; set; } = "CLP";
    public DateTime DataInstalacao { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Operacional";
    public string? Observacoes { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}
