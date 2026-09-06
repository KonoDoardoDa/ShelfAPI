using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShelfApi.Models;
using ShelfApi.Repositories;

namespace ShelfApi.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITokenService _tokenService;

    public AuthService(IUsuarioRepository usuarioRepository, ITokenService tokenService)
    {
        _usuarioRepository = usuarioRepository;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
    {
        if (string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Senha))
        {
            return null;
        }

        var usuario = await _usuarioRepository.GetByEmailAsync(loginDto.Email.Trim());
        if (usuario is null || !usuario.Ativo)
        {
            return null;
        }

        var isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Senha, usuario.SenhaHash);
        if (!isPasswordValid)
        {
            return null;
        }

        var (token, expiresAt) = _tokenService.GenerateToken(usuario);

        return new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            Usuario = new UsuarioResponseDto
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Role = usuario.Role,
                Ativo = usuario.Ativo,
                DataCriacao = usuario.DataCriacao
            }
        };
    }

    public async Task<UsuarioResponseDto?> RegisterAsync(RegisterDto registerDto)
    {
        if (string.IsNullOrWhiteSpace(registerDto.Email) || string.IsNullOrWhiteSpace(registerDto.Senha) || string.IsNullOrWhiteSpace(registerDto.Nome))
        {
            return null;
        }

        var existingUser = await _usuarioRepository.GetByEmailAsync(registerDto.Email.Trim());
        if (existingUser != null)
        {
            return null; // Usuário já existente com este e-mail
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(registerDto.Senha);
        var novoUsuario = new Usuario
        {
            Nome = registerDto.Nome.Trim(),
            Email = registerDto.Email.Trim(),
            SenhaHash = hash,
            Role = string.IsNullOrWhiteSpace(registerDto.Role) ? "Operador" : registerDto.Role.Trim(),
            Ativo = true,
            DataCriacao = DateTime.UtcNow
        };

        var createdId = await _usuarioRepository.CreateAsync(novoUsuario);
        novoUsuario.Id = createdId;

        return new UsuarioResponseDto
        {
            Id = novoUsuario.Id,
            Nome = novoUsuario.Nome,
            Email = novoUsuario.Email,
            Role = novoUsuario.Role,
            Ativo = novoUsuario.Ativo,
            DataCriacao = novoUsuario.DataCriacao
        };
    }

    public async Task<UsuarioResponseDto?> GetUserByIdAsync(int id)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id);
        if (usuario is null) return null;

        return new UsuarioResponseDto
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            Role = usuario.Role,
            Ativo = usuario.Ativo,
            DataCriacao = usuario.DataCriacao
        };
    }

    public async Task<IEnumerable<UsuarioResponseDto>> GetAllUsersAsync()
    {
        var usuarios = await _usuarioRepository.GetAllAsync();
        return usuarios.Select(u => new UsuarioResponseDto
        {
            Id = u.Id,
            Nome = u.Nome,
            Email = u.Email,
            Role = u.Role,
            Ativo = u.Ativo,
            DataCriacao = u.DataCriacao
        });
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        return await _usuarioRepository.DeleteAsync(id);
    }
}
