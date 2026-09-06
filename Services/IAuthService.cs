using System.Collections.Generic;
using System.Threading.Tasks;
using ShelfApi.Models;

namespace ShelfApi.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
    Task<UsuarioResponseDto?> RegisterAsync(RegisterDto registerDto);
    Task<UsuarioResponseDto?> GetUserByIdAsync(int id);
    Task<IEnumerable<UsuarioResponseDto>> GetAllUsersAsync();
    Task<bool> DeleteUserAsync(int id);
}
