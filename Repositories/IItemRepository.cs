using ShelfApi.Models;

namespace ShelfApi.Repositories;

public interface IItemRepository
{
    Task<IEnumerable<Item>> GetAllAsync();
    Task<Item?> GetItemByIdAsync(int id);
    Task<int> CreateItem(Item item);
    Task<bool> UpdateItem(Item item);
    Task<bool> DeleteItem(int id);
}
