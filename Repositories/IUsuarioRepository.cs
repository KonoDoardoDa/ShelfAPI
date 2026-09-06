using System.Collections.Generic;
using System.Threading.Tasks;
using ShelfApi.Models;

namespace ShelfApi.Repositories;

public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> GetAllAsync();
    Task<Usuario?> GetByIdAsync(int id);
    Task<Usuario?> GetByEmailAsync(string email);
    Task<int> CreateAsync(Usuario usuario);
    Task<bool> UpdateAsync(Usuario usuario);
    Task<bool> DeleteAsync(int id);
}
