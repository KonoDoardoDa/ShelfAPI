using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using ShelfApi.Infrastructure;
using ShelfApi.Models;

namespace ShelfApi.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly IDbConnectionFactory _factory;

    public UsuarioRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<Usuario>> GetAllAsync()
    {
        var sql = "SELECT Id, Nome, Email, SenhaHash, Role, Ativo, DataCriacao FROM usuario ORDER BY Id DESC;";
        using var db = _factory.CreateConnection();
        return await db.QueryAsync<Usuario>(sql);
    }

    public async Task<Usuario?> GetByIdAsync(int id)
    {
        var sql = "SELECT Id, Nome, Email, SenhaHash, Role, Ativo, DataCriacao FROM usuario WHERE Id = @id;";
        using var db = _factory.CreateConnection();
        return await db.QueryFirstOrDefaultAsync<Usuario>(sql, new { id });
    }

    public async Task<Usuario?> GetByEmailAsync(string email)
    {
        var sql = "SELECT Id, Nome, Email, SenhaHash, Role, Ativo, DataCriacao FROM usuario WHERE LOWER(Email) = LOWER(@email);";
        using var db = _factory.CreateConnection();
        return await db.QueryFirstOrDefaultAsync<Usuario>(sql, new { email });
    }

    public async Task<int> CreateAsync(Usuario usuario)
    {
        var sql = @"
            INSERT INTO usuario (Nome, Email, SenhaHash, Role, Ativo, DataCriacao)
            VALUES (@Nome, @Email, @SenhaHash, @Role, @Ativo, @DataCriacao);
            SELECT LAST_INSERT_ID();";

        if (usuario.DataCriacao == default)
        {
            usuario.DataCriacao = DateTime.UtcNow;
        }

        using var db = _factory.CreateConnection();
        return await db.ExecuteScalarAsync<int>(sql, usuario);
    }

    public async Task<bool> UpdateAsync(Usuario usuario)
    {
        var sql = @"
            UPDATE usuario
            SET Nome = @Nome,
                Email = @Email,
                Role = @Role,
                Ativo = @Ativo
            WHERE Id = @Id;";

        using var db = _factory.CreateConnection();
        var rows = await db.ExecuteAsync(sql, usuario);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var sql = "DELETE FROM usuario WHERE Id = @id;";
        using var db = _factory.CreateConnection();
        var rows = await db.ExecuteAsync(sql, new { id });
        return rows > 0;
    }
}
