using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelfApi.Models;
using ShelfApi.Services;

namespace ShelfApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EstoqueKitController : ControllerBase
{
    private readonly IEstoqueKitService _service;

    public EstoqueKitController(IEstoqueKitService service)
    {
        _service = service;
    }

    [HttpGet("Saldos")]
    public async Task<ActionResult<IEnumerable<KitSaldoDto>>> GetAllSaldos()
    {
        var saldos = await _service.GetAllSaldosAsync();
        return Ok(saldos);
    }

    [HttpGet("Saldo/{kitId}")]
    public async Task<ActionResult<KitSaldoDto>> GetSaldo(int kitId)
    {
        var saldo = await _service.GetSaldoByIdAsync(kitId);
        if (saldo == null)
        {
            return NotFound(new { message = $"Kit #{kitId} não encontrado." });
        }
        return Ok(saldo);
    }

    [HttpPost("Montar")]
    public async Task<ActionResult<ResultadoOperacaoKitDto>> Montar([FromBody] MontarKitRequestDto dto)
    {
        if (dto == null || dto.KitId <= 0 || dto.Quantidade <= 0)
        {
            return BadRequest(new { message = "Kit e quantidade válida (> 0) são obrigatórios." });
        }

        try
        {
            if (string.IsNullOrWhiteSpace(dto.UsuarioNome))
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
                dto.UsuarioNome = userName ?? "Operador";
            }

            if (dto.UsuarioId == null)
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value;
                if (int.TryParse(idClaim, out int uid))
                {
                    dto.UsuarioId = uid;
                }
            }

            var resultado = await _service.MontarKitAsync(dto);
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Erro ao montar kit: {ex.Message}" });
        }
    }

    [HttpPost("Desmontar")]
    public async Task<ActionResult<ResultadoOperacaoKitDto>> Desmontar([FromBody] DesmontarKitRequestDto dto)
    {
        if (dto == null || dto.KitId <= 0 || dto.Quantidade <= 0)
        {
            return BadRequest(new { message = "Kit e quantidade válida (> 0) são obrigatórios." });
        }

        try
        {
            if (string.IsNullOrWhiteSpace(dto.UsuarioNome))
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
                dto.UsuarioNome = userName ?? "Operador";
            }

            if (dto.UsuarioId == null)
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value;
                if (int.TryParse(idClaim, out int uid))
                {
                    dto.UsuarioId = uid;
                }
            }

            var resultado = await _service.DesmontarKitAsync(dto);
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Erro ao desmontar kit: {ex.Message}" });
        }
    }

    [HttpPost("Movimentar")]
    public async Task<ActionResult<ResultadoOperacaoKitDto>> Movimentar([FromBody] MovimentarKitManualDto dto)
    {
        if (dto == null || dto.KitId <= 0 || dto.Quantidade <= 0)
        {
            return BadRequest(new { message = "Kit e quantidade válida (> 0) são obrigatórios." });
        }

        try
        {
            if (string.IsNullOrWhiteSpace(dto.UsuarioNome))
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
                dto.UsuarioNome = userName ?? "Operador";
            }

            if (dto.UsuarioId == null)
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value;
                if (int.TryParse(idClaim, out int uid))
                {
                    dto.UsuarioId = uid;
                }
            }

            var resultado = await _service.MovimentarManualAsync(dto);
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Erro na movimentação: {ex.Message}" });
        }
    }

    [HttpGet("Historico")]
    public async Task<ActionResult<IEnumerable<MovimentacaoKitDetalheDto>>> GetHistorico([FromQuery] int? kitId, [FromQuery] int limit = 100)
    {
        var historico = await _service.GetHistoricoAsync(kitId, limit);
        return Ok(historico);
    }

    [HttpPost("Simular")]
    public async Task<ActionResult<SimulacaoProducaoResultadoDto>> Simular([FromBody] List<ItemSimulacaoInputDto> itensSimulacao)
    {
        if (itensSimulacao == null || itensSimulacao.Count == 0)
        {
            return Ok(new SimulacaoProducaoResultadoDto { ProducaoTotalmenteViavel = true });
        }

        try
        {
            var resultado = await _service.SimularProducaoAsync(itensSimulacao);
            return Ok(resultado);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Erro ao simular produção: {ex.Message}" });
        }
    }
}
