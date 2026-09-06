using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelfApi.Models;
using ShelfApi.Services;

namespace ShelfApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("Login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        if (loginDto == null || string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Senha))
        {
            return BadRequest(new { message = "E-mail e senha são obrigatórios." });
        }

        var result = await _authService.LoginAsync(loginDto);
        if (result == null)
        {
            return Unauthorized(new { message = "E-mail ou senha incorretos." });
        }

        return Ok(result);
    }

    [HttpPost("Register")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UsuarioResponseDto>> Register([FromBody] RegisterDto registerDto)
    {
        if (registerDto == null || string.IsNullOrWhiteSpace(registerDto.Email) || string.IsNullOrWhiteSpace(registerDto.Senha) || string.IsNullOrWhiteSpace(registerDto.Nome))
        {
            return BadRequest(new { message = "Nome, e-mail e senha são obrigatórios." });
        }

        var result = await _authService.RegisterAsync(registerDto);
        if (result == null)
        {
            return BadRequest(new { message = "Este endereço de e-mail já está em uso por outro usuário." });
        }

        return Ok(result);
    }

    [HttpGet("Me")]
    [Authorize]
    public async Task<ActionResult<UsuarioResponseDto>> GetMe()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value;
        if (!int.TryParse(idClaim, out var userId))
        {
            return Unauthorized();
        }

        var usuario = await _authService.GetUserByIdAsync(userId);
        if (usuario == null)
        {
            return NotFound();
        }

        return Ok(usuario);
    }

    [HttpGet("Users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetAllUsers()
    {
        var users = await _authService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpDelete("Users/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var success = await _authService.DeleteUserAsync(id);
        if (!success)
        {
            return NotFound(new { message = "Usuário não encontrado." });
        }

        return NoContent();
    }
}
