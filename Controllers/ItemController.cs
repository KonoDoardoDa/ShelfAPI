using Microsoft.AspNetCore.Mvc;
using ShelfApi.Exceptions;
using ShelfApi.Models;
using ShelfApi.Services;
using System;
using System.Threading.Tasks;

namespace ShelfApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ItemController : ControllerBase
{
    private readonly IItemService _itemService;

    public ItemController(IItemService itemService)
    {
        _itemService = itemService;
    }

    [HttpGet]
    [Route("GetAllItems")]
    public async Task<IActionResult> GetAllItems()
    {
        var items = await _itemService.GetAllItemAsync();
        return Ok(items);
    }

    [HttpGet]
    [Route("GetItemById")]
    public async Task<IActionResult> GetItemById(int id)
    {
        var item = await _itemService.GetItemByIdAsync(id);

        if (item == null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    [HttpPost]
    [Route("CreateItem")]
    public async Task<IActionResult> CreateItem([FromBody] Item item)
    {
        try
        {
            var id = await _itemService.CreateItem(item);

            if (id == 0)
            {
                return BadRequest();
            }
            return Ok(id);
        }
        catch (InexistentProviderException)
        {
            return NotFound($"O fornecedorId: {item.FornecedorId} não existe.");
        }
    }

    [HttpPut]
    [Route("UpdateItem")]
    public async Task<IActionResult> UpdateItem([FromBody] Item item)
    {
        try
        {
            await _itemService.UpdateItem(item);
            return NoContent();
        }
        catch (ItemNotFoundException)
        {
            return NotFound($"O item: {item.Nome} não foi encontrado.");
        }
    }

    [HttpPatch]
    [Route("ToggleStatus/{id}")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var success = await _itemService.ToggleStatusAsync(id);
        if (!success)
        {
            return NotFound(new { message = "Item não encontrado." });
        }
        return Ok(new { message = "Status do item alterado com sucesso." });
    }

    [HttpDelete]
    [Route("DeleteItem")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        try
        {
            await _itemService.DeleteItem(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ItemNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Erro ao excluir item: {ex.Message}" });
        }
    }
}
