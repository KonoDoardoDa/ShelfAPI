using System;
using ShelfApi.Models;

namespace ShelfApi.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(Usuario usuario);
}
