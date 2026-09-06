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
        const string sql = @"SELECT Id, Nome FROM Kit;";

        using var db = _factory.CreateConnection();
        return await db.QueryAsync<Kit>(sql);
    }

    public async Task<Kit?> GetKitByIdAsync(int id)
    {
        const string sql = @"SELECT Id, Nome
                             FROM Kit
                             WHERE Id = @id;";

        using var db = _factory.CreateConnection();
        return await db.QueryFirstOrDefaultAsync<Kit>(sql, new { id });
    }

    public async Task<int> CreateKit(Kit kit)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@nome", kit.Nome);

        const string sql = @"
            INSERT INTO Kit (Nome)
            VALUES (@nome);
            SELECT LAST_INSERT_ID();";

        using var db = _factory.CreateConnection();

        return await db.ExecuteScalarAsync<int>(sql, parameters);
    }

    public async Task<bool> UpdateKit(Kit kit)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@id", kit.Id);
        parameters.Add("@nome", kit.Nome);

        const string sql = @"
            UPDATE Kit
            SET Nome = @nome
            WHERE Id = @id;";

        using var db = _factory.CreateConnection();
        var affectedRows = await db.ExecuteAsync(sql, parameters);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteKit(int id)
    {
        const string sql = "DELETE FROM Kit WHERE Id = @Id;";

        using var db = _factory.CreateConnection();
        var affectedRows = await db.ExecuteAsync(sql, new { Id = id });
        return affectedRows > 0;
    }
    public async Task<KitDetails?> GetKitWithItemsAsync(int id)
    {
        const string sql = @"
            SELECT
                k.Id AS Id,
                k.Nome AS Nome,
                i.Id AS ItemId,
                i.Nome AS ItemNome,
                ki.Quantidade AS Quantidade,
                ki.IsPainel AS IsPainel
            FROM Kit k
            LEFT JOIN KitItem ki ON k.Id = ki.KitId
            LEFT JOIN Item i ON ki.ItemId = i.Id
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