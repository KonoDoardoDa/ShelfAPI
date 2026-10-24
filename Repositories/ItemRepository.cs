using Dapper;
using ShelfApi.Infrastructure;
using ShelfApi.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
                i.Ativo,
                COALESCE(e.quantidade, 0) AS QuantidadeEstoque 
            FROM item i 
            LEFT JOIN estoque e ON i.Id = e.itemId
            ORDER BY i.Id ASC;";
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
                i.Ativo,
                COALESCE(e.quantidade, 0) AS QuantidadeEstoque 
            FROM item i 
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
        parameters.Add("@ativo", item.Ativo);

        string sql = @"
            INSERT INTO item (nome, FornecedorId, Descricao, Ativo)
            VALUES (@nome, @fornecedorId, @descricao, @ativo);
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
        parameters.Add("@ativo", item.Ativo);

        string sql = @"
            UPDATE item
            SET nome = @nome,
                fornecedorId = @fornecedorId,
                descricao = @descricao,
                Ativo = @ativo
            WHERE id = @id;";
        using var db = _factory.CreateConnection();
        var affectedRows = await db.ExecuteAsync(sql, parameters);

        return affectedRows > 0;
    }

    public async Task<bool> DeleteItem(int id)
    {
        using var db = _factory.CreateConnection();

        // Verificar histórico de movimentações ou vinculação com kits
        const string checkSql = @"
            SELECT 
                (SELECT COUNT(1) FROM movimentacao_estoque WHERE itemId = @id) +
                (SELECT COUNT(1) FROM kititem WHERE ItemId = @id) AS Total;";
        var totalLinks = await db.ExecuteScalarAsync<int>(checkSql, new { id });

        if (totalLinks > 0)
        {
            throw new InvalidOperationException($"Não é possível excluir o item #{id} pois ele possui histórico de movimentação ou está vinculado a kits. Utilize a opção Inativar.");
        }

        // Remover saldo do estoque associado
        await db.ExecuteAsync("DELETE FROM estoque WHERE itemId = @Id;", new { Id = id });

        var sql = "DELETE FROM item WHERE Id = @Id;";
        var affectedRows = await db.ExecuteAsync(sql, new { Id = id });

        return affectedRows > 0;
    }

    public async Task<bool> ToggleStatus(int id)
    {
        const string sql = "UPDATE item SET Ativo = NOT Ativo WHERE Id = @id;";
        using var db = _factory.CreateConnection();
        return await db.ExecuteAsync(sql, new { id }) > 0;
    }
}
