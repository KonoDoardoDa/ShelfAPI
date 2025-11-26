using ShelfApi.Models;
namespace ShelfApi.Repositories
{
    public interface IFornecedorRepository
    {
        Task<IEnumerable<Fornecedor>> GetAllAsync();
        Task<int> CreateFornecedor(Fornecedor fornecedor);
        Task<int> UpdateFornecedor(Fornecedor fornecedor);
        Task<int> DeleteFornecedor(int fornecedorId);
    }
}
