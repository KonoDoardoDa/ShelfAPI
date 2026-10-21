using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ShelfApi.Exceptions;
using ShelfApi.Models;
using ShelfApi.Services;

namespace ShelfApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KitItemController : ControllerBase
    {
        private readonly IKitItemService _kitItemService;

        public KitItemController(IKitItemService kitItemService)
        {
            _kitItemService = kitItemService;
        }

        [HttpGet]
        [Route("GetAll")]
        public async Task<ActionResult<IEnumerable<KitItem>>> GetAll()
        {
            var items = await _kitItemService.GetAllAsync();
            return Ok(items);
        }

        [HttpGet]
        [Route("GetByKit")]
        public async Task<ActionResult<IEnumerable<KitItem>>> GetByKit(int kitId)
        {
            var items = await _kitItemService.GetItemsByKitAsync(kitId);
            return Ok(items);
        }

        [HttpGet]
        [Route("GetById")]
        public async Task<ActionResult<KitItem>> GetById(int kitId, int itemId)
        {
            var kitItem = await _kitItemService.GetKitItemAsync(kitId, itemId);
            if (kitItem is null)
            {
                return NotFound();
            }

            return Ok(kitItem);
        }

        [HttpPost]
        [Route("Create")]
        public async Task<ActionResult> Create([FromBody] KitItem kitItem)
        {
            if (kitItem is null)
            {
                return BadRequest(new { message = "Dados do item não informados." });
            }

            if (kitItem.KitId <= 0 || kitItem.ItemId <= 0)
            {
                return BadRequest(new { message = "Identificador do kit ou do item inválido." });
            }

            const int maxQuantidade = 1_000_000;
            if (kitItem.Quantidade <= 0 || kitItem.Quantidade > maxQuantidade)
            {
                return BadRequest(new { message = $"A quantidade do item no kit deve ser entre 1 e {maxQuantidade:N0} unidades." });
            }

            // Verificar se o item já existe na composição do kit
            var existingItem = await _kitItemService.GetKitItemAsync(kitItem.KitId, kitItem.ItemId);
            if (existingItem != null)
            {
                return Conflict(new
                {
                    message = $"Este item já existe na composição deste kit (Quantidade atual: {existingItem.Quantidade} un). É preferível incrementar a quantidade do item existente em vez de adicioná-lo novamente."
                });
            }

            try
            {
                await _kitItemService.CreateKitItemAsync(kitItem);
                return CreatedAtAction(nameof(GetById), new { kitId = kitItem.KitId, itemId = kitItem.ItemId }, kitItem);
            }
            catch (DuplicateKitItemException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = $"Erro interno ao adicionar item ao kit: {ex.Message}" });
            }
        }

        [HttpPut]
        [Route("Update")]
        public async Task<ActionResult> Update(int kitId, int itemId, [FromBody] KitItem kitItem)
        {
            if (kitItem is null || kitItem.KitId != kitId || kitItem.ItemId != itemId)
            {
                return BadRequest(new { message = "Dados do item inconsistentes com a requisição." });
            }

            const int maxQuantidade = 1_000_000;
            if (kitItem.Quantidade <= 0 || kitItem.Quantidade > maxQuantidade)
            {
                return BadRequest(new { message = $"A quantidade do item no kit deve ser entre 1 e {maxQuantidade:N0} unidades." });
            }

            try
            {
                await _kitItemService.UpdateKitItemAsync(kitItem);
                return NoContent();
            }
            catch (KitItemNotFoundException)
            {
                return NotFound(new { message = $"Item #{itemId} não encontrado na composição do kit #{kitId}." });
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = $"Erro interno ao atualizar item do kit: {ex.Message}" });
            }
        }

        [HttpDelete]
        [Route("Delete")]
        public async Task<ActionResult> Delete(int kitId, int itemId)
        {
            try
            {
                await _kitItemService.DeleteKitItemAsync(kitId, itemId);
                return NoContent();
            }
            catch (KitItemNotFoundException)
            {
                return NotFound();
            }
        }
    }
}