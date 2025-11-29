using System.Collections.Generic;
using System.Threading.Tasks;
using ShelfApi.Models;

namespace ShelfApi.Repositories;

public interface IKitRepository
{
    Task<IEnumerable<Kit>> GetAllAsync();
    Task<Kit?> GetKitByIdAsync(int id);
    Task<int> CreateKit(Kit kit);
    Task<bool> UpdateKit(Kit kit);
    Task<bool> DeleteKit(int id);
    Task<KitDetails?> GetKitWithItemsAsync(int id);
}