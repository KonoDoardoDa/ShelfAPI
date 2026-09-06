using System.Collections.Generic;
using System.Threading.Tasks;
using ShelfApi.Models;
using ShelfApi.Repositories;

namespace ShelfApi.Services;

public class EstoqueKitService : IEstoqueKitService
{
    private readonly IEstoqueKitRepository _repository;

    public EstoqueKitService(IEstoqueKitRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<KitSaldoDto>> GetAllSaldosAsync()
    {
        return await _repository.GetAllSaldosAsync();
    }

    public async Task<KitSaldoDto?> GetSaldoByIdAsync(int kitId)
    {
        return await _repository.GetSaldoByIdAsync(kitId);
    }

    public async Task<ResultadoOperacaoKitDto> MontarKitAsync(MontarKitRequestDto dto)
    {
        return await _repository.MontarKitAsync(
            dto.KitId,
            dto.Quantidade,
            dto.Motivo,
            dto.UsuarioId,
            dto.UsuarioNome ?? "Sistema"
        );
    }

    public async Task<ResultadoOperacaoKitDto> DesmontarKitAsync(DesmontarKitRequestDto dto)
    {
        return await _repository.DesmontarKitAsync(
            dto.KitId,
            dto.Quantidade,
            dto.Motivo,
            dto.UsuarioId,
            dto.UsuarioNome ?? "Sistema"
        );
    }

    public async Task<ResultadoOperacaoKitDto> MovimentarManualAsync(MovimentarKitManualDto dto)
    {
        return await _repository.MovimentarManualAsync(
            dto.KitId,
            dto.Tipo,
            dto.Quantidade,
            dto.Motivo,
            dto.UsuarioId,
            dto.UsuarioNome ?? "Sistema"
        );
    }

    public async Task<IEnumerable<MovimentacaoKitDetalheDto>> GetHistoricoAsync(int? kitId = null, int limit = 100)
    {
        return await _repository.GetHistoricoAsync(kitId, limit);
    }

    public async Task<SimulacaoProducaoResultadoDto> SimularProducaoAsync(IEnumerable<ItemSimulacaoInputDto> itensSimulacao)
    {
        return await _repository.SimularProducaoAsync(itensSimulacao);
    }
}
