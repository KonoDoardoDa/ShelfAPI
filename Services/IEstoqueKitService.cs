using System.Collections.Generic;
using System.Threading.Tasks;
using ShelfApi.Models;

namespace ShelfApi.Services;

public interface IEstoqueKitService
{
    Task<IEnumerable<KitSaldoDto>> GetAllSaldosAsync();
    Task<KitSaldoDto?> GetSaldoByIdAsync(int kitId);
    Task<ResultadoOperacaoKitDto> MontarKitAsync(MontarKitRequestDto dto);
    Task<ResultadoOperacaoKitDto> DesmontarKitAsync(DesmontarKitRequestDto dto);
    Task<ResultadoOperacaoKitDto> MovimentarManualAsync(MovimentarKitManualDto dto);
    Task<IEnumerable<MovimentacaoKitDetalheDto>> GetHistoricoAsync(int? kitId = null, int limit = 100);
    Task<SimulacaoProducaoResultadoDto> SimularProducaoAsync(IEnumerable<ItemSimulacaoInputDto> itensSimulacao);
}
