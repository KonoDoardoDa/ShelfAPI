using System.Collections.Generic;
using System.Threading.Tasks;
using ShelfApi.Models;

namespace ShelfApi.Services;

public interface IKitItemService
{
    Task<IEnumerable<KitItem>> GetAllAsync();
    Task<IEnumerable<KitItem>> GetItemsByKitAsync(int kitId);
    Task<KitItem?> GetKitItemAsync(int kitId, int itemId);
    Task CreateKitItemAsync(KitItem kitItem);
    Task UpdateKitItemAsync(KitItem kitItem);
    Task DeleteKitItemAsync(int kitId, int itemId);
}