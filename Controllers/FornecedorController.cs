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
    public class FornecedorController : ControllerBase
    {
        private readonly IFornecedorService _fornecedorService;

        public FornecedorController(IFornecedorService fornecedorService)
        {
            _fornecedorService = fornecedorService;
        }

        [HttpGet]
        [Route("GetAll")]
        public async Task<ActionResult<IEnumerable<Fornecedor>>> GetAll()
        {
            var fornecedores = await _fornecedorService.GetAllFornecedoresAsync();
            return Ok(fornecedores);
        }

        [HttpGet]
        [Route("GetById")]
        public async Task<ActionResult<Fornecedor>> GetById(int id)
        {
            var fornecedor = await _fornecedorService.GetFornecedorByIdAsync(id);
            if (fornecedor is null)
            {
                return NotFound();
            }

            return Ok(fornecedor);
        }

        [HttpPost]
        [Route("Create")]
        public async Task<ActionResult> Create([FromBody] Fornecedor fornecedor)
        {
            if (fornecedor is null)
            {
                return BadRequest();
            }

            var createdId = await _fornecedorService.CreateFornecedor(fornecedor);
            return CreatedAtAction(nameof(GetById), new { id = createdId }, fornecedor);
        }

        [HttpPut]
        [Route("Update")]
        public async Task<ActionResult> Update(int id, [FromBody] Fornecedor fornecedor)
        {
            if (fornecedor is null || fornecedor.Id != id)
            {
                return BadRequest();
            }

            try
            {
                var updatedRows = await _fornecedorService.UpdateFornecedorAsync(fornecedor);
                if (updatedRows == 0)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (InexistentProviderException)
            {
                return NotFound();
            }
        }

        [HttpDelete]
        [Route("Delete")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                await _fornecedorService.DeleteFornecedorAsync(id);
                return NoContent();
            }
            catch (InexistentProviderException)
            {
                return NotFound();
            }
        }
    }
}