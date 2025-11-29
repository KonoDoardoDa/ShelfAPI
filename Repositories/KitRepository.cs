using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ShelfApi.Models;
using System.Data;

namespace ShelfApi.Repositories;

public class KitRepository : IKitRepository
{
    private readonly IDbConnection _db;

    public KitRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Kit>> GetAllAsync()
    {
        const string sql = @"SELECT Id, Nome FROM Kit;";
        return await _db.QueryAsync<Kit>(sql);
    }

    public async Task<Kit?> GetKitByIdAsync(int id)
    {
        const string sql = @"SELECT Id, Nome
                             FROM Kit
                             WHERE Id = @id;";
        return await _db.QueryFirstOrDefaultAsync<Kit>(sql, new { id });
    }

    public async Task<int> CreateKit(Kit kit)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@nome", kit.Nome);

        const string sql = @"
            INSERT INTO Kit (Nome)
            VALUES (@nome);
            SELECT LAST_INSERT_ID();";

        return await _db.ExecuteScalarAsync<int>(sql, parameters);
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

        var affectedRows = await _db.ExecuteAsync(sql, parameters);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteKit(int id)
    {
        const string sql = "DELETE FROM Kit WHERE Id = @Id;";
        var affectedRows = await _db.ExecuteAsync(sql, new { Id = id });
        return affectedRows > 0;
    }

    // New: returns kit plus item names and quantities (single query with join)
    public async Task<KitDetails?> GetKitWithItemsAsync(int id)
    {
        const string sql = @"
            SELECT
                k.Id AS Id,
                k.Nome AS Nome,
                i.Id AS ItemId,
                i.Nome AS ItemNome,
                ki.Quantidade AS Quantidade
            FROM Kit k
            LEFT JOIN KitItem ki ON k.Id = ki.KitId
            LEFT JOIN Item i ON ki.ItemId = i.Id
            WHERE k.Id = @id;";

        var kitDict = new Dictionary<int, KitDetails>();

        var mapped = await _db.QueryAsync<KitDetails, KitItemDetail, KitDetails>(
            sql,
            (kit, kitItem) =>
            {
                if (!kitDict.TryGetValue(kit.Id, out var kitEntry))
                {
                    kitEntry = kit;
                    kitEntry.Items = new List<KitItemDetail>();
                    kitDict.Add(kitEntry.Id, kitEntry);
                }

                // If no matching item row, ItemId will be 0 (default). Skip in that case.
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