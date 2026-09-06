using System.Collections.Generic;
using System.Threading.Tasks;
using ShelfApi.Models;

namespace ShelfApi.Services;

public interface ISoftwareService
{
    Task<IEnumerable<Software>> GetAllAsync();
    Task<Software?> GetByIdAsync(int id);
    Task<int> CreateAsync(Software software);
    Task<bool> UpdateAsync(int id, Software software);
    Task<bool> DeleteAsync(int id);
}
