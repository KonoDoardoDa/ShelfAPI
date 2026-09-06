using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShelfApi.Models;
using ShelfApi.Repositories;

namespace ShelfApi.Services;

public class EstoqueService : IEstoqueService
{
    private readonly IEstoqueRepository _estoqueRepository;

    public EstoqueService(IEstoqueRepository estoqueRepository)
    {
        _estoqueRepository = estoqueRepository;
    }

    public async Task<int> GetSaldoByItemIdAsync(int itemId)
    {
        return await _estoqueRepository.GetSaldoByItemIdAsync(itemId);
    }

    public async Task<IEnumerable<EstoqueSaldoDto>> GetAllSaldosAsync()
    {
        return await _estoqueRepository.GetAllSaldosAsync();
    }

    public async Task<int> MovimentarEstoqueAsync(MovimentacaoEstoqueDto dto, int? usuarioId = null, string? usuarioNome = null)
    {
        if (dto.ItemId <= 0)
        {
            throw new ArgumentException("ID do item inválido.");
        }

        if (dto.Quantidade <= 0)
        {
            throw new ArgumentException("A quantidade movimentada deve ser maior que zero.");
        }

        var isEntrada = string.Equals(dto.Tipo, "Entrada", StringComparison.OrdinalIgnoreCase);
        var isSaida = string.Equals(dto.Tipo, "Saida", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(dto.Tipo, "Saída", StringComparison.OrdinalIgnoreCase);

        if (!isEntrada && !isSaida)
        {
            throw new ArgumentException("Tipo de movimentação inválido. Utilize 'Entrada' ou 'Saida'.");
        }

        var delta = isEntrada ? dto.Quantidade : -dto.Quantidade;
        var tipoNormalizado = isEntrada ? "Entrada" : "Saida";

        var idFinal = usuarioId ?? dto.UsuarioId;
        var nomeFinal = !string.IsNullOrWhiteSpace(usuarioNome)
            ? usuarioNome
            : (!string.IsNullOrWhiteSpace(dto.UsuarioNome) ? dto.UsuarioNome : "Sistema");

        return await _estoqueRepository.MovimentarAsync(
            dto.ItemId,
            delta,
            tipoNormalizado,
            dto.Quantidade,
            dto.Motivo,
            idFinal,
            nomeFinal
        );
    }

    public async Task<IEnumerable<MovimentacaoEstoqueDetalheDto>> GetHistoricoAsync(int? itemId = null, int limit = 100)
    {
        if (limit <= 0) limit = 100;
        if (limit > 500) limit = 500;

        return await _estoqueRepository.GetHistoricoAsync(itemId, limit);
    }
}
