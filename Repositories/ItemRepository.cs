using System;
using Dapper;
using ShelfApi.Models;
using System.Data;
using System.Reflection.Metadata;

namespace ShelfApi.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly IDbConnection _db;

    public ItemRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Item>> GetAllAsync()
    {
        var sql = @"SELECT Id, Nome, FornecedorId FROM Item;";
        return await _db.QueryAsync<Item>(sql);
    }

    public async Task<Item?> GetItemByIdAsync(int id)
    {
        var sql = @"SELECT Id, nome, FornecedorId 
                    FROM Item 
                    WHERE Id = @id;";
        return await _db.QueryFirstOrDefaultAsync<Item>(sql, new { id });
    }

    public async Task<int> CreateItem(Item item)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@nome", item.Nome);
        parameters.Add("@fornecedorId", item.FornecedorId);

        string sql = @"INSERT INTO Item (nome, FornecedorId)
                    VALUES (@nome, @fornecedorId);
                    SELECT LAST_INSERT_ID()";

        return await _db.ExecuteScalarAsync<int>(sql, parameters);
    }

    public async Task<bool> UpdateItem(Item item)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@id", item.Id);
        parameters.Add("@nome", item.Nome);
        parameters.Add("@fornecedorId", item.FornecedorId);

        string sql = @"UPDATE Item
                    SET nome = @nome
                    AND fornecedorId = @fornecedorId
                    WHERE id= @id";

        var affectedRows = await _db.ExecuteAsync(sql, parameters);

        return affectedRows > 0;
    }

    public async Task<bool> DeleteItem(int id)
    {
        var sql = "DELETE FROM Item WHERE Id = @Id;";

        var affectedRows = await _db.ExecuteAsync(sql, new { Id = id });

        return affectedRows > 0;
    }
}
