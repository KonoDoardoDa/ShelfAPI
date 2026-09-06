using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ShelfApi.Models;
using ShelfApi.Services;

namespace ShelfApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SoftwareController : ControllerBase
{
    private readonly ISoftwareService _softwareService;

    public SoftwareController(ISoftwareService softwareService)
    {
        _softwareService = softwareService;
    }

    [HttpGet]
    [Route("GetAll")]
    public async Task<ActionResult<IEnumerable<Software>>> GetAll()
    {
        var result = await _softwareService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet]
    [Route("GetById")]
    public async Task<ActionResult<Software>> GetById([FromQuery] int id)
    {
        var software = await _softwareService.GetByIdAsync(id);
        if (software == null)
            return NotFound(new { message = $"Software com ID {id} não encontrado." });

        return Ok(software);
    }

    [HttpPost]
    [Route("Create")]
    public async Task<ActionResult> Create([FromBody] Software software)
    {
        if (software == null)
            return BadRequest(new { message = "Dados inválidos." });

        try
        {
            var createdId = await _softwareService.CreateAsync(software);
            software.Id = createdId;
            return CreatedAtAction(nameof(GetById), new { id = createdId }, software);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut]
    [Route("Update")]
    public async Task<ActionResult> Update([FromQuery] int id, [FromBody] Software software)
    {
        if (software == null)
            return BadRequest(new { message = "Dados inválidos." });

        try
        {
            var success = await _softwareService.UpdateAsync(id, software);
            if (!success)
                return NotFound(new { message = $"Software com ID {id} não encontrado." });

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete]
    [Route("Delete")]
    public async Task<ActionResult> Delete([FromQuery] int id)
    {
        var success = await _softwareService.DeleteAsync(id);
        if (!success)
            return NotFound(new { message = $"Software com ID {id} não encontrado." });

        return NoContent();
    }
}
