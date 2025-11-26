using ShelfApi.Models;

namespace ShelfApi.Services
{
    public interface IFornecedorService
    {
        Task<IEnumerable<Fornecedor>> GetAllFornecedoresAsync();
        Task<Fornecedor?> GetFornecedorByIdAsync(int id);
        Task<int> CreateFornecedor(Fornecedor fornecedor);
        Task<int> UpdateFornecedorAsync(Fornecedor fornecedor);
        Task DeleteFornecedorAsync(int id);
    }
}
