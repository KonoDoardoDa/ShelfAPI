using System.Collections.Generic;
using System.Threading.Tasks;
using ShelfApi.Exceptions;
using ShelfApi.Models;
using ShelfApi.Repositories;

namespace ShelfApi.Services;

public class KitService : IKitService
{
    private readonly IKitRepository _kitRepository;

    public KitService(IKitRepository kitRepository)
    {
        _kitRepository = kitRepository;
    }

    public async Task<IEnumerable<Kit>> GetAllKitsAsync()
    {
        return await _kitRepository.GetAllAsync();
    }

    public async Task<Kit?> GetKitByIdAsync(int id)
    {
        return await _kitRepository.GetKitByIdAsync(id);
    }

    public async Task<int> CreateKit(Kit kit)
    {
        return await _kitRepository.CreateKit(kit);
    }

    public async Task<int> UpdateKitAsync(Kit kit)
    {
        var updated = await _kitRepository.UpdateKit(kit);
        if (!updated)
        {
            throw new KitNotFoundException(kit.Id);
        }

        return kit.Id;
    }

    public async Task DeleteKitAsync(int id)
    {
        var deleted = await _kitRepository.DeleteKit(id);
        if (!deleted)
        {
            throw new KitNotFoundException(id);
        }
    }

    public async Task<KitDetails?> GetKitWithItemsAsync(int id)
    {
        return await _kitRepository.GetKitWithItemsAsync(id);
    }
}