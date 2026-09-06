using System;

namespace ShelfApi.Models;

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}

public class RegisterDto
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string Role { get; set; } = "Operador";
}

public class UsuarioResponseDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Operador";
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; }
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UsuarioResponseDto Usuario { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}
