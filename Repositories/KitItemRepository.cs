using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using ShelfApi.Models;
using System.Data;
using ShelfApi.Infrastructure;


namespace ShelfApi.Repositories;

public class KitItemRepository : IKitItemRepository
{
    private readonly IDbConnectionFactory _factory;

    public KitItemRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<KitItem>> GetAllAsync()
    {
        const string sql = @"SELECT KitId, ItemId, Quantidade, IsPainel FROM KitItem;";
        using var db = _factory.CreateConnection();
        return await db.QueryAsync<KitItem>(sql);
    }

    public async Task<IEnumerable<KitItem>> GetByKitIdAsync(int kitId)
    {
        const string sql = @"SELECT KitId, ItemId, Quantidade, IsPainel
                             FROM KitItem
                             WHERE KitId = @kitId;";
        using var db = _factory.CreateConnection();
        return await db.QueryAsync<KitItem>(sql, new { kitId });
    }

    public async Task<KitItem?> GetKitItemAsync(int kitId, int itemId)
    {
        const string sql = @"SELECT KitId, ItemId, Quantidade, IsPainel
                             FROM KitItem
                             WHERE KitId = @kitId AND ItemId = @itemId;";

        using var db = _factory.CreateConnection();
        return await db.QueryFirstOrDefaultAsync<KitItem>(sql, new { kitId, itemId });
    }

    public async Task<bool> CreateKitItem(KitItem kitItem)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@kitId", kitItem.KitId);
        parameters.Add("@itemId", kitItem.ItemId);
        parameters.Add("@quantidade", kitItem.Quantidade);
        parameters.Add("@isPainel", kitItem.IsPainel);

        const string sql = @"
            INSERT INTO KitItem (KitId, ItemId, Quantidade, IsPainel)
            VALUES (@kitId, @itemId, @quantidade, @isPainel);";

        using var db = _factory.CreateConnection();
        var affectedRows = await db.ExecuteAsync(sql, parameters);
        return affectedRows > 0;
    }

    public async Task<bool> UpdateKitItem(KitItem kitItem)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@kitId", kitItem.KitId);
        parameters.Add("@itemId", kitItem.ItemId);
        parameters.Add("@quantidade", kitItem.Quantidade);
        parameters.Add("@isPainel", kitItem.IsPainel);

        const string sql = @"
            UPDATE KitItem
            SET Quantidade = @quantidade, IsPainel = @isPainel
            WHERE KitId = @kitId AND ItemId = @itemId;";

        using var db = _factory.CreateConnection();
        var affectedRows = await db.ExecuteAsync(sql, parameters);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteKitItem(int kitId, int itemId)
    {
        const string sql = @"DELETE FROM KitItem WHERE KitId = @kitId AND ItemId = @itemId;";

        using var db = _factory.CreateConnection();
        var affectedRows = await db.ExecuteAsync(sql, new { kitId, itemId });
        return affectedRows > 0;
    }
}