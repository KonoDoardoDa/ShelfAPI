using System;
using System.Collections.Generic;

namespace ShelfApi.Models;

public class EstoqueKit
{
    public int KitId { get; set; }
    public int Quantidade { get; set; }
}

public class KitSaldoDto
{
    public int KitId { get; set; }
    public string NomeKit { get; set; } = string.Empty;
    public int SaldoFisico { get; set; }
    public int CapacidadeMontagem { get; set; }
    public int TotalItensComposicao { get; set; }
    public List<KitItemDisponibilidadeDto> Itens { get; set; } = new();
}

public class KitItemDisponibilidadeDto
{
    public int ItemId { get; set; }
    public string NomeItem { get; set; } = string.Empty;
    public int QuantidadeNecessaria { get; set; }
    public int SaldoEstoqueItem { get; set; }
    public bool IsPainel { get; set; }
    public int MontagensPossiveis => QuantidadeNecessaria > 0 ? SaldoEstoqueItem / QuantidadeNecessaria : 0;
}

public class MontarKitRequestDto
{
    public int KitId { get; set; }
    public int Quantidade { get; set; }
    public string? Motivo { get; set; }
    public int? UsuarioId { get; set; }
    public string? UsuarioNome { get; set; }
}

public class DesmontarKitRequestDto
{
    public int KitId { get; set; }
    public int Quantidade { get; set; }
    public string? Motivo { get; set; }
    public int? UsuarioId { get; set; }
    public string? UsuarioNome { get; set; }
}

public class MovimentarKitManualDto
{
    public int KitId { get; set; }
    public string Tipo { get; set; } = "Entrada"; // "Entrada" ou "Saida"
    public int Quantidade { get; set; }
    public string? Motivo { get; set; }
    public int? UsuarioId { get; set; }
    public string? UsuarioNome { get; set; }
}

public class MovimentacaoKitDetalheDto
{
    public int Id { get; set; }
    public int KitId { get; set; }
    public string NomeKit { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public int SaldoAnterior { get; set; }
    public int SaldoPosterior { get; set; }
    public string? Motivo { get; set; }
    public int? UsuarioId { get; set; }
    public string UsuarioNome { get; set; } = "Sistema";
    public DateTime DataMovimentacao { get; set; }
}

public class ResultadoOperacaoKitDto
{
    public int KitId { get; set; }
    public string NomeKit { get; set; } = string.Empty;
    public int NovoSaldoKit { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}

public class ItemSimulacaoInputDto
{
    public int KitId { get; set; }
    public int Quantidade { get; set; }
}

public class ItemSimulacaoKitOrigemDto
{
    public int KitId { get; set; }
    public string NomeKit { get; set; } = string.Empty;
    public int QuantidadeKit { get; set; }
    public int QuantidadePorKit { get; set; }
    public int SubtotalNecessario { get; set; }
    public bool IsPainel { get; set; }
}

public class ItemSimulacaoResultadoDto
{
    public int ItemId { get; set; }
    public string NomeItem { get; set; } = string.Empty;
    public int QuantidadeTotalNecessaria { get; set; }
    public int SaldoEstoqueItem { get; set; }
    public int SaldoAposMontagem { get; set; }
    public int FaltaEstoque { get; set; }
    public bool Suficiente { get; set; }
    public bool IsPainel { get; set; }
    public List<ItemSimulacaoKitOrigemDto> KitsOrigem { get; set; } = new();
}

public class SimulacaoProducaoResultadoDto
{
    public int TotalKitsSolicitados { get; set; }
    public int TotalTiposKits { get; set; }
    public int TotalComponentesDistintos { get; set; }
    public int TotalComponentesAtendidos { get; set; }
    public int TotalComponentesFaltantes { get; set; }
    public bool ProducaoTotalmenteViavel { get; set; }
    public List<ItemSimulacaoResultadoDto> Itens { get; set; } = new();
}

