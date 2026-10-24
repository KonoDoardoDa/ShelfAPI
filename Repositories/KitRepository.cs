using Dapper;
using ShelfApi.Models;
using System.Data;
using ShelfApi.Infrastructure;

namespace ShelfApi.Repositories;

public class KitRepository : IKitRepository
{
    private readonly IDbConnectionFactory _factory;

    public KitRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<Kit>> GetAllAsync()
    {
        const string sql = @"SELECT Id, Nome, Ativo FROM kit ORDER BY Id ASC;";

        using var db = _factory.CreateConnection();
        return await db.QueryAsync<Kit>(sql);
    }

    public async Task<Kit?> GetKitByIdAsync(int id)
    {
        const string sql = @"SELECT Id, Nome, Ativo
                             FROM kit
                             WHERE Id = @id;";

        using var db = _factory.CreateConnection();
        return await db.QueryFirstOrDefaultAsync<Kit>(sql, new { id });
    }

    public async Task<int> CreateKit(Kit kit)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@nome", kit.Nome);
        parameters.Add("@ativo", kit.Ativo);

        const string sql = @"
            INSERT INTO kit (Nome, Ativo)
            VALUES (@nome, @ativo);
            SELECT LAST_INSERT_ID();";

        using var db = _factory.CreateConnection();

        return await db.ExecuteScalarAsync<int>(sql, parameters);
    }

    public async Task<bool> UpdateKit(Kit kit)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@id", kit.Id);
        parameters.Add("@nome", kit.Nome);
        parameters.Add("@ativo", kit.Ativo);

        const string sql = @"
            UPDATE kit
            SET Nome = @nome,
                Ativo = @ativo
            WHERE Id = @id;";

        using var db = _factory.CreateConnection();
        var affectedRows = await db.ExecuteAsync(sql, parameters);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteKit(int id)
    {
        using var db = _factory.CreateConnection();

        // Verificar se existem vínculos na composição, histórico de movimentação, instalações em softwares ou saldo físico
        const string checkSql = @"
            SELECT 
                (SELECT COUNT(1) FROM kititem WHERE KitId = @id) +
                (SELECT COUNT(1) FROM movimentacao_kit WHERE kitId = @id) +
                (SELECT COUNT(1) FROM software WHERE KitId = @id) +
                (SELECT COALESCE(SUM(quantidade), 0) FROM estoque_kit WHERE kitId = @id) AS Total;";
        var totalLinks = await db.ExecuteScalarAsync<int>(checkSql, new { id });

        if (totalLinks > 0)
        {
            throw new InvalidOperationException($"Não é possível excluir o kit #{id} pois ele possui itens em sua composição, histórico de montagens ou instalações vinculadas. Utilize a opção Inativar.");
        }

        // Se for kit virgem (sem histórico e sem saldo), remove itens da composição e saldo zerado
        await db.ExecuteAsync("DELETE FROM kititem WHERE KitId = @id;", new { id });
        await db.ExecuteAsync("DELETE FROM estoque_kit WHERE kitId = @id;", new { id });

        const string sql = "DELETE FROM kit WHERE Id = @Id;";
        var affectedRows = await db.ExecuteAsync(sql, new { Id = id });
        return affectedRows > 0;
    }

    public async Task<bool> ToggleStatus(int id)
    {
        const string sql = "UPDATE kit SET Ativo = NOT Ativo WHERE Id = @id;";
        using var db = _factory.CreateConnection();
        return await db.ExecuteAsync(sql, new { id }) > 0;
    }

    public async Task<KitDetails?> GetKitWithItemsAsync(int id)
    {
        const string sql = @"
            SELECT
                k.Id AS Id,
                k.Nome AS Nome,
                k.Ativo AS Ativo,
                i.Id AS ItemId,
                i.Nome AS ItemNome,
                ki.Quantidade AS Quantidade,
                ki.IsPainel AS IsPainel
            FROM kit k
            LEFT JOIN kititem ki ON k.Id = ki.KitId
            LEFT JOIN item i ON ki.ItemId = i.Id
            WHERE k.Id = @id;";

        var kitDict = new Dictionary<int, KitDetails>();

        using var db = _factory.CreateConnection();

        var mapped = await db.QueryAsync<KitDetails, KitItemDetail, KitDetails>(
            sql,
            (kit, kitItem) =>
            {
                if (!kitDict.TryGetValue(kit.Id, out var kitEntry))
                {
                    kitEntry = kit;
                    kitEntry.Items = new List<KitItemDetail>();
                    kitDict.Add(kitEntry.Id, kitEntry);
                }

                if (kitItem != null && kitItem.ItemId != 0)
                {
                    kitEntry.Items.Add(kitItem);
                }

                return kitEntry;
            },
            new { id },
            splitOn: "ItemId"
        );

        return kitDict.Values.FirstOrDefault();
    }
}