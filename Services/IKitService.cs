using System.Collections.Generic;
using System.Threading.Tasks;
using ShelfApi.Models;

namespace ShelfApi.Services;

public interface IKitService
{
    Task<IEnumerable<Kit>> GetAllKitsAsync();
    Task<Kit?> GetKitByIdAsync(int id);
    Task<int> CreateKit(Kit kit);
    Task<int> UpdateKitAsync(Kit kit);
    Task DeleteKitAsync(int id);
    Task<KitDetails?> GetKitWithItemsAsync(int id);
}