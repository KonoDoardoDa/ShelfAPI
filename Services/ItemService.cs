using System;
using ShelfApi.Models;
using ShelfApi.Repositories;
using ShelfApi.Services;
using ShelfApi.Exceptions;

namespace ShelfApi.Services;

public class ItemService : IItemService
{
    private IItemRepository _itemRepository;
    private IFornecedorRepository _fornecedorRepository;
    public ItemService(IItemRepository itemRepository, IFornecedorRepository fornecedorRepository)
    {
        _itemRepository = itemRepository;
        _fornecedorRepository = fornecedorRepository;
    }

    public async Task<IEnumerable<Item>> GetAllItemAsync()
    {
        return await _itemRepository.GetAllAsync();
    }

    public async Task<Item?> GetItemByIdAsync(int id)
    {
        var item = await _itemRepository.GetItemByIdAsync(id);

        if (item == null)
        {
            throw new ItemNotFoundException($"Nenhum item encontrado para o seguinte id: {id}");
        }
        return item;
    }

    public async Task<int> CreateItem(Item item)
    {
        var providers = await _fornecedorRepository.GetAllAsync();

        if (!providers.Any(p => p.Id == item.FornecedorId))
        {
            throw new InexistentProviderException($"Não há nenhum fornecedor com este id: {item.FornecedorId}.");
        }

        return await _itemRepository.CreateItem(item);
    }

    public async Task UpdateItem(Item item)
    {
        var success = await _itemRepository.UpdateItem(item);

        if (!success)
        {
            throw new ItemNotFoundException($"Nenhum item encontrado para o seguinte id: {item.Id}");
        }
    }

    public async Task DeleteItem(int id)
    {
        var success = await _itemRepository.DeleteItem(id);

        if (!success)
        {
            throw new ItemNotFoundException($"Nenhum item encontrado para o seguinte id: {id}");
        }
    }
}
