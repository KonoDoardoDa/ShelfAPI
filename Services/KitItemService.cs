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
        var created = await _kitItemRepository.CreateKitItem(kitItem);
        if (!created)
        {
            throw new Exception("Failed to create kit item.");
        }
    }

    public async Task UpdateKitItemAsync(KitItem kitItem)
    {
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