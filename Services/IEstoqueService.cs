using System.Collections.Generic;
using System.Threading.Tasks;
using ShelfApi.Models;

namespace ShelfApi.Services;

public interface IEstoqueService
{
    Task<int> GetSaldoByItemIdAsync(int itemId);
    Task<IEnumerable<EstoqueSaldoDto>> GetAllSaldosAsync();
    Task<int> MovimentarEstoqueAsync(MovimentacaoEstoqueDto dto, int? usuarioId = null, string? usuarioNome = null);
    Task<IEnumerable<MovimentacaoEstoqueDetalheDto>> GetHistoricoAsync(int? itemId = null, int limit = 100);
}
