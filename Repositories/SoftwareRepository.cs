using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using ShelfApi.Infrastructure;
using ShelfApi.Models;

namespace ShelfApi.Repositories;

public class SoftwareRepository : ISoftwareRepository
{
    private readonly IDbConnectionFactory _factory;

    public SoftwareRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<Software>> GetAllAsync()
    {
        using var db = _factory.CreateConnection();
        const string sql = @"
            SELECT 
                s.Id, 
                s.NomeSoftware, 
                s.Versao, 
                s.Cliente, 
                s.KitId, 
                COALESCE(k.Nome, s.NomeKit) AS NomeKit,
                s.TipoDispositivo, 
                s.DataInstalacao, 
                s.Status, 
                s.Observacoes, 
                s.DataCriacao
            FROM software s
            LEFT JOIN kit k ON s.KitId = k.Id
            ORDER BY s.DataInstalacao DESC;";
        return await db.QueryAsync<Software>(sql);
    }

    public async Task<Software?> GetByIdAsync(int id)
    {
        using var db = _factory.CreateConnection();
        const string sql = @"
            SELECT 
                s.Id, 
                s.NomeSoftware, 
                s.Versao, 
                s.Cliente, 
                s.KitId, 
                COALESCE(k.Nome, s.NomeKit) AS NomeKit,
                s.TipoDispositivo, 
                s.DataInstalacao, 
                s.Status, 
                s.Observacoes, 
                s.DataCriacao
            FROM software s
            LEFT JOIN kit k ON s.KitId = k.Id
            WHERE s.Id = @Id;";
        return await db.QueryFirstOrDefaultAsync<Software>(sql, new { Id = id });
    }

    public async Task<int> CreateAsync(Software software)
    {
        using var db = _factory.CreateConnection();
        const string sql = @"
            INSERT INTO software (NomeSoftware, Versao, Cliente, KitId, NomeKit, TipoDispositivo, DataInstalacao, Status, Observacoes, DataCriacao)
            VALUES (@NomeSoftware, @Versao, @Cliente, @KitId, @NomeKit, @TipoDispositivo, @DataInstalacao, @Status, @Observacoes, @DataCriacao);
            SELECT LAST_INSERT_ID();";
        
        if (software.DataCriacao == default)
        {
            software.DataCriacao = DateTime.UtcNow;
        }

        return await db.ExecuteScalarAsync<int>(sql, software);
    }

    public async Task<bool> UpdateAsync(Software software)
    {
        using var db = _factory.CreateConnection();
        const string sql = @"
            UPDATE software 
            SET NomeSoftware = @NomeSoftware,
                Versao = @Versao,
                Cliente = @Cliente,
                KitId = @KitId,
                NomeKit = @NomeKit,
                TipoDispositivo = @TipoDispositivo,
                DataInstalacao = @DataInstalacao,
                Status = @Status,
                Observacoes = @Observacoes
            WHERE Id = @Id;";
        var rows = await db.ExecuteAsync(sql, software);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var db = _factory.CreateConnection();
        const string sql = "DELETE FROM software WHERE Id = @Id;";
        var rows = await db.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }
}
