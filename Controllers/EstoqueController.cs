using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ShelfApi.Models;
using ShelfApi.Services;

namespace ShelfApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstoqueController : ControllerBase
{
    private readonly IEstoqueService _estoqueService;

    public EstoqueController(IEstoqueService estoqueService)
    {
        _estoqueService = estoqueService;
    }

    [HttpGet("GetSaldo")]
    public async Task<IActionResult> GetSaldo([FromQuery] int itemId)
    {
        if (itemId <= 0)
        {
            return BadRequest(new { message = "ID do item inválido." });
        }

        var saldo = await _estoqueService.GetSaldoByItemIdAsync(itemId);
        return Ok(new { itemId, saldo });
    }

    [HttpGet("GetAllSaldos")]
    public async Task<IActionResult> GetAllSaldos()
    {
        var saldos = await _estoqueService.GetAllSaldosAsync();
        return Ok(saldos);
    }

    [HttpPost("Movimentar")]
    public async Task<IActionResult> Movimentar([FromBody] MovimentacaoEstoqueDto dto)
    {
        if (dto == null)
        {
            return BadRequest(new { message = "Dados da movimentação não informados." });
        }

        try
        {
            // Extrair usuário logado das claims do JWT (se houver autenticação)
            int? usuarioId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                              User.FindFirst("userId")?.Value;
            if (int.TryParse(userIdClaim, out var parsedId))
            {
                usuarioId = parsedId;
            }

            var usuarioNome = User.FindFirst(ClaimTypes.Name)?.Value ??
                              User.Identity?.Name ??
                              dto.UsuarioNome ??
                              "Sistema";

            var novoSaldo = await _estoqueService.MovimentarEstoqueAsync(dto, usuarioId, usuarioNome);
            return Ok(new
            {
                itemId = dto.ItemId,
                novoSaldo,
                tipo = dto.Tipo,
                quantidade = dto.Quantidade,
                motivo = dto.Motivo,
                notaFiscal = dto.NotaFiscal,
                usuarioNome,
                message = $"Estoque atualizado com sucesso. Novo saldo: {novoSaldo} unidades."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Erro interno ao movimentar estoque: {ex.Message}" });
        }
    }

    [HttpGet("Historico")]
    public async Task<IActionResult> GetHistorico([FromQuery] int? itemId = null, [FromQuery] int limit = 100)
    {
        try
        {
            var historico = await _estoqueService.GetHistoricoAsync(itemId, limit);
            return Ok(historico);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Erro ao obter histórico de movimentações: {ex.Message}" });
        }
    }

    [HttpPost("ImportarLote")]
    public async Task<IActionResult> ImportarLote([FromBody] ImportarLoteRequestDto dto)
    {
        if (dto == null || dto.Itens == null || dto.Itens.Count == 0)
        {
            return BadRequest(new { message = "Nenhum item informado para importação." });
        }

        try
        {
            int? usuarioId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                              User.FindFirst("userId")?.Value;
            if (int.TryParse(userIdClaim, out var parsedId))
            {
                usuarioId = parsedId;
            }

            var usuarioNome = User.FindFirst(ClaimTypes.Name)?.Value ??
                              User.Identity?.Name ??
                              dto.UsuarioNome ??
                              "Sistema";

            var resultado = await _estoqueService.ImportarLoteAsync(dto, usuarioId, usuarioNome);
            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Erro ao importar lote de itens: {ex.Message}" });
        }
    }
}
