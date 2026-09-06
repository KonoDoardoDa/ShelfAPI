using System.Collections.Generic;
using System.Threading.Tasks;
using ShelfApi.Models;

namespace ShelfApi.Repositories;

public interface IEstoqueRepository
{
    Task<int> GetSaldoByItemIdAsync(int itemId);
    Task<IEnumerable<EstoqueSaldoDto>> GetAllSaldosAsync();
    Task<int> MovimentarAsync(int itemId, int delta, string tipo, int quantidade, string? motivo, int? usuarioId, string usuarioNome);
    Task<IEnumerable<MovimentacaoEstoqueDetalheDto>> GetHistoricoAsync(int? itemId = null, int limit = 100);
}
