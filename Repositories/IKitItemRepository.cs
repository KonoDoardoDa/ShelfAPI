using System.Collections.Generic;
using System.Threading.Tasks;
using ShelfApi.Models;

namespace ShelfApi.Repositories;

public interface IKitItemRepository
{
    Task<IEnumerable<KitItem>> GetAllAsync();
    Task<IEnumerable<KitItem>> GetByKitIdAsync(int kitId);
    Task<KitItem?> GetKitItemAsync(int kitId, int itemId);
    Task<bool> CreateKitItem(KitItem kitItem);
    Task<bool> UpdateKitItem(KitItem kitItem);
    Task<bool> DeleteKitItem(int kitId, int itemId);
}