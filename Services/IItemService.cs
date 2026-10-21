using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShelfApi.Models;

namespace ShelfApi.Services;

public interface IItemService
{
    Task<IEnumerable<Item>> GetAllItemAsync();
    Task<Item?> GetItemByIdAsync(int id);
    Task<int> CreateItem(Item item);
    Task UpdateItem(Item item);
    Task DeleteItem(int id);
    Task<bool> ToggleStatusAsync(int id);
}
