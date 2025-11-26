using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ShelfApi.Exceptions;
using ShelfApi.Models;
using ShelfApi.Services;
using System;

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

    [HttpDelete]
    [Route("DeleteItem")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        try
        {
            await _itemService.DeleteItem(id);
            return NoContent();
        }
        catch (ItemNotFoundException)
        {
            return NotFound();
        }
    }
}