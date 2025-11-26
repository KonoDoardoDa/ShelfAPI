using ShelfApi.Repositories;
using ShelfApi.Models;

namespace ShelfApi.Services
{
    public class FornecedorService : IFornecedorService
    {
        private readonly IFornecedorRepository _fornecedorRepository;
        public FornecedorService(IFornecedorRepository fornecedorRepository)
        {
            _fornecedorRepository = fornecedorRepository;
        }

        public async Task<IEnumerable<Fornecedor>> GetAllFornecedoresAsync()
        {
            return await _fornecedorRepository.GetAllAsync();
        }

        public async Task<Fornecedor?> GetFornecedorByIdAsync(int id)
        {
            var fornecedores = await _fornecedorRepository.GetAllAsync();
            return fornecedores.FirstOrDefault(f => f.Id == id);
        }
        public async Task<int> CreateFornecedor(Fornecedor fornecedor)
        {
            return await _fornecedorRepository.CreateFornecedor(fornecedor);
        }
        public async Task<int> UpdateFornecedorAsync(Fornecedor fornecedor)
        {
            return await _fornecedorRepository.UpdateFornecedor(fornecedor);
        }

        public async Task DeleteFornecedorAsync(int id)
        {
            await _fornecedorRepository.DeleteFornecedor(id);
        }
    }
}
