using System.Collections.Generic;
using System.Threading.Tasks;
using ShelfApi.Exceptions;
using ShelfApi.Models;
using ShelfApi.Repositories;

namespace ShelfApi.Services;

public class KitItemService : IKitItemService
{
    private readonly IKitItemRepository _kitItemRepository;

    public KitItemService(IKitItemRepository kitItemRepository)
    {
        _kitItemRepository = kitItemRepository;
    }

    public async Task<IEnumerable<KitItem>> GetAllAsync()
    {
        return await _kitItemRepository.GetAllAsync();
    }

    public async Task<IEnumerable<KitItem>> GetItemsByKitAsync(int kitId)
    {
        return await _kitItemRepository.GetByKitIdAsync(kitId);
    }

    public async Task<KitItem?> GetKitItemAsync(int kitId, int itemId)
    {
        return await _kitItemRepository.GetKitItemAsync(kitId, itemId);
    }

    public async Task CreateKitItemAsync(KitItem kitItem)
    {
        const int maxQuantidade = 1_000_000;
        if (kitItem.Quantidade <= 0 || kitItem.Quantidade > maxQuantidade)
        {
            throw new System.ArgumentException($"A quantidade do item no kit deve ser entre 1 e {maxQuantidade:N0} unidades.");
        }

        var created = await _kitItemRepository.CreateKitItem(kitItem);
        if (!created)
        {
            throw new Exception("Falha ao adicionar item ao kit.");
        }
    }

    public async Task UpdateKitItemAsync(KitItem kitItem)
    {
        const int maxQuantidade = 1_000_000;
        if (kitItem.Quantidade <= 0 || kitItem.Quantidade > maxQuantidade)
        {
            throw new System.ArgumentException($"A quantidade do item no kit deve ser entre 1 e {maxQuantidade:N0} unidades.");
        }

        var updated = await _kitItemRepository.UpdateKitItem(kitItem);
        if (!updated)
        {
            throw new KitItemNotFoundException(kitItem.KitId, kitItem.ItemId);
        }
    }

    public async Task DeleteKitItemAsync(int kitId, int itemId)
    {
        var deleted = await _kitItemRepository.DeleteKitItem(kitId, itemId);
        if (!deleted)
        {
            throw new KitItemNotFoundException(kitId, itemId);
        }
    }
}