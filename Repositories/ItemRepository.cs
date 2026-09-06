using Dapper;
using ShelfApi.Infrastructure;
using ShelfApi.Models;

namespace ShelfApi.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly IDbConnectionFactory _factory;

    public ItemRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<Item>> GetAllAsync()
    {
        var sql = @"
            SELECT 
                i.Id, 
                i.Nome, 
                i.FornecedorId, 
                i.Descricao, 
                COALESCE(e.quantidade, 0) AS QuantidadeEstoque 
            FROM Item i 
            LEFT JOIN estoque e ON i.Id = e.itemId;";
        using var db = _factory.CreateConnection();
        return await db.QueryAsync<Item>(sql);
    }

    public async Task<Item?> GetItemByIdAsync(int id)
    {
        var sql = @"
            SELECT 
                i.Id, 
                i.Nome, 
                i.FornecedorId, 
                i.Descricao, 
                COALESCE(e.quantidade, 0) AS QuantidadeEstoque 
            FROM Item i 
            LEFT JOIN estoque e ON i.Id = e.itemId 
            WHERE i.Id = @id;";
        using var db = _factory.CreateConnection();
        return await db.QueryFirstOrDefaultAsync<Item>(sql, new { id });
    }

    public async Task<int> CreateItem(Item item)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@nome", item.Nome);
        parameters.Add("@fornecedorId", item.FornecedorId);
        parameters.Add("@descricao", item.Descricao);

        string sql = @"
            INSERT INTO Item (nome, FornecedorId, Descricao)
            VALUES (@nome, @fornecedorId, @descricao);
            SELECT LAST_INSERT_ID();";
        using var db = _factory.CreateConnection();

        var newId = await db.ExecuteScalarAsync<int>(sql, parameters);

        // Inicializar saldo em estoque caso não exista
        if (newId > 0)
        {
            var sqlEstoque = @"
                INSERT INTO estoque (itemId, quantidade) 
                VALUES (@newId, 0)
                ON DUPLICATE KEY UPDATE itemId = itemId;";
            await db.ExecuteAsync(sqlEstoque, new { newId });
        }

        return newId;
    }

    public async Task<bool> UpdateItem(Item item)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@id", item.Id);
        parameters.Add("@nome", item.Nome);
        parameters.Add("@fornecedorId", item.FornecedorId);
        parameters.Add("@descricao", item.Descricao);

        string sql = @"
            UPDATE Item
            SET nome = @nome,
                fornecedorId = @fornecedorId,
                descricao = @descricao
            WHERE id = @id;";
        using var db = _factory.CreateConnection();
        var affectedRows = await db.ExecuteAsync(sql, parameters);

        return affectedRows > 0;
    }

    public async Task<bool> DeleteItem(int id)
    {
        using var db = _factory.CreateConnection();

        // Remover saldo do estoque associado
        await db.ExecuteAsync("DELETE FROM estoque WHERE itemId = @Id;", new { Id = id });

        var sql = "DELETE FROM Item WHERE Id = @Id;";
        var affectedRows = await db.ExecuteAsync(sql, new { Id = id });

        return affectedRows > 0;
    }
}
