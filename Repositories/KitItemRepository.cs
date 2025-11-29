using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using ShelfApi.Models;
using System.Data;

namespace ShelfApi.Repositories;

public class KitItemRepository : IKitItemRepository
{
    private readonly IDbConnection _db;

    public KitItemRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<IEnumerable<KitItem>> GetAllAsync()
    {
        const string sql = @"SELECT KitId, ItemId, Quantidade FROM KitItem;";
        return await _db.QueryAsync<KitItem>(sql);
    }

    public async Task<IEnumerable<KitItem>> GetByKitIdAsync(int kitId)
    {
        const string sql = @"SELECT KitId, ItemId, Quantidade
                             FROM KitItem
                             WHERE KitId = @kitId;";
        return await _db.QueryAsync<KitItem>(sql, new { kitId });
    }

    public async Task<KitItem?> GetKitItemAsync(int kitId, int itemId)
    {
        const string sql = @"SELECT KitId, ItemId, Quantidade
                             FROM KitItem
                             WHERE KitId = @kitId AND ItemId = @itemId;";
        return await _db.QueryFirstOrDefaultAsync<KitItem>(sql, new { kitId, itemId });
    }

    public async Task<bool> CreateKitItem(KitItem kitItem)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@kitId", kitItem.KitId);
        parameters.Add("@itemId", kitItem.ItemId);
        parameters.Add("@quantidade", kitItem.Quantidade);

        const string sql = @"
            INSERT INTO KitItem (KitId, ItemId, Quantidade)
            VALUES (@kitId, @itemId, @quantidade);";

        var affectedRows = await _db.ExecuteAsync(sql, parameters);
        return affectedRows > 0;
    }

    public async Task<bool> UpdateKitItem(KitItem kitItem)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@kitId", kitItem.KitId);
        parameters.Add("@itemId", kitItem.ItemId);
        parameters.Add("@quantidade", kitItem.Quantidade);

        const string sql = @"
            UPDATE KitItem
            SET Quantidade = @quantidade
            WHERE KitId = @kitId AND ItemId = @itemId;";

        var affectedRows = await _db.ExecuteAsync(sql, parameters);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteKitItem(int kitId, int itemId)
    {
        const string sql = @"DELETE FROM KitItem WHERE KitId = @kitId AND ItemId = @itemId;";
        var affectedRows = await _db.ExecuteAsync(sql, new { kitId, itemId });
        return affectedRows > 0;
    }
}