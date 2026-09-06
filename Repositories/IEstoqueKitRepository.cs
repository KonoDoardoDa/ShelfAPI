using System.Collections.Generic;
using System.Threading.Tasks;
using ShelfApi.Models;

namespace ShelfApi.Repositories;

public interface IEstoqueKitRepository
{
    Task<IEnumerable<KitSaldoDto>> GetAllSaldosAsync();
    Task<KitSaldoDto?> GetSaldoByIdAsync(int kitId);
    Task<ResultadoOperacaoKitDto> MontarKitAsync(int kitId, int quantidade, string? motivo, int? usuarioId, string usuarioNome);
    Task<ResultadoOperacaoKitDto> DesmontarKitAsync(int kitId, int quantidade, string? motivo, int? usuarioId, string usuarioNome);
    Task<ResultadoOperacaoKitDto> MovimentarManualAsync(int kitId, string tipo, int quantidade, string? motivo, int? usuarioId, string usuarioNome);
    Task<IEnumerable<MovimentacaoKitDetalheDto>> GetHistoricoAsync(int? kitId = null, int limit = 100);
    Task<SimulacaoProducaoResultadoDto> SimularProducaoAsync(IEnumerable<ItemSimulacaoInputDto> itensSimulacao);
}
