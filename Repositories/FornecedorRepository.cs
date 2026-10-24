using Dapper;
using ShelfApi.Infrastructure;
using ShelfApi.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ShelfApi.Repositories
{
    public class FornecedorRepository : IFornecedorRepository
    {
        private readonly IDbConnectionFactory _factory;

        public FornecedorRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IEnumerable<Fornecedor>> GetAllAsync()
        {
            const string sql = "SELECT Id, Nome, Ativo FROM fornecedor ORDER BY Id ASC;";
            using var db = _factory.CreateConnection();
            return await db.QueryAsync<Fornecedor>(sql);
        }

        public async Task<int> CreateFornecedor(Fornecedor fornecedor)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@nome", fornecedor.Nome);
            parameters.Add("@ativo", fornecedor.Ativo);
            const string sql = @"INSERT INTO fornecedor (Nome, Ativo)
                                 VALUES (@nome, @ativo);
                                 SELECT LAST_INSERT_ID();";

            using var db = _factory.CreateConnection();
            return await db.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<int> UpdateFornecedor(Fornecedor fornecedor)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@id", fornecedor.Id);
            parameters.Add("@nome", fornecedor.Nome);
            parameters.Add("@ativo", fornecedor.Ativo);
            const string sql = @"UPDATE fornecedor
                                 SET Nome = @nome,
                                     Ativo = @ativo
                                 WHERE Id = @id;";
            using var db = _factory.CreateConnection();
            return await db.ExecuteAsync(sql, parameters);
        }

        public async Task<bool> DeleteFornecedor(int fornecedorId)
        {
            using var db = _factory.CreateConnection();

            // Verificar se existem itens vinculados a este fornecedor
            const string checkSql = "SELECT COUNT(1) FROM item WHERE FornecedorId = @id;";
            var linkedItems = await db.ExecuteScalarAsync<int>(checkSql, new { id = fornecedorId });

            if (linkedItems > 0)
            {
                throw new InvalidOperationException($"Não é possível excluir o fornecedor #{fornecedorId} pois existem {linkedItems} item(ns) vinculados a ele. Utilize a opção Inativar.");
            }

            const string sql = @"DELETE FROM fornecedor WHERE Id = @id;";
            var affectedRows = await db.ExecuteAsync(sql, new { id = fornecedorId });
            return affectedRows > 0;
        }

        public async Task<bool> ToggleStatus(int id)
        {
            const string sql = "UPDATE fornecedor SET Ativo = NOT Ativo WHERE Id = @id;";
            using var db = _factory.CreateConnection();
            return await db.ExecuteAsync(sql, new { id }) > 0;
        }
    }
}
