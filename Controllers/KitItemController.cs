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
                return BadRequest();
            }

            await _kitItemService.CreateKitItemAsync(kitItem);
            return CreatedAtAction(nameof(GetById), new { kitId = kitItem.KitId, itemId = kitItem.ItemId }, kitItem);
        }

        [HttpPut]
        [Route("Update")]
        public async Task<ActionResult> Update(int kitId, int itemId, [FromBody] KitItem kitItem)
        {
            if (kitItem is null || kitItem.KitId != kitId || kitItem.ItemId != itemId)
            {
                return BadRequest();
            }

            try
            {
                await _kitItemService.UpdateKitItemAsync(kitItem);
                return NoContent();
            }
            catch (KitItemNotFoundException)
            {
                return NotFound();
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