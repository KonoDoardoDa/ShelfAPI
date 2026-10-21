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
    public class KitController : ControllerBase
    {
        private readonly IKitService _kitService;

        public KitController(IKitService kitService)
        {
            _kitService = kitService;
        }

        [HttpGet]
        [Route("GetAll")]
        public async Task<ActionResult<IEnumerable<Kit>>> GetAll()
        {
            var kits = await _kitService.GetAllKitsAsync();
            return Ok(kits);
        }

        [HttpGet]
        [Route("GetById")]
        public async Task<ActionResult<Kit>> GetById(int id)
        {
            var kit = await _kitService.GetKitByIdAsync(id);
            if (kit is null)
            {
                return NotFound();
            }

            return Ok(kit);
        }

        [HttpPost]
        [Route("Create")]
        public async Task<ActionResult> Create([FromBody] Kit kit)
        {
            if (kit is null)
            {
                return BadRequest();
            }

            var createdId = await _kitService.CreateKit(kit);
            kit.Id = createdId;
            return CreatedAtAction(nameof(GetById), new { id = createdId }, kit);
        }

        [HttpPut]
        [Route("Update")]
        public async Task<ActionResult> Update(int id, [FromBody] Kit kit)
        {
            if (kit is null || kit.Id != id)
            {
                return BadRequest();
            }

            try
            {
                await _kitService.UpdateKitAsync(kit);
                return NoContent();
            }
            catch (KitNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPatch]
        [Route("ToggleStatus/{id:int}")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var success = await _kitService.ToggleStatusAsync(id);
            if (!success)
            {
                return NotFound(new { message = "Kit não encontrado." });
            }
            return Ok(new { message = "Status do kit alterado com sucesso." });
        }

        [HttpDelete]
        [Route("Delete")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                await _kitService.DeleteKitAsync(id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KitNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Erro ao excluir kit: {ex.Message}" });
            }
        }

        [HttpGet]
        [Route("GetWithItems/{id:int}")]
        public async Task<ActionResult<KitDetails>> GetWithItems(int id)
        {
            var kit = await _kitService.GetKitWithItemsAsync(id);
            if (kit is null)
            {
                return NotFound();
            }

            return Ok(kit);
        }
    }
}