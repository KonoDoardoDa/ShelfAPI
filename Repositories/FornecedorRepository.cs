using Dapper;
using ShelfApi.Infrastructure;
using ShelfApi.Models;
using System.Data;

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
            const string sql = "SELECT Id, Nome FROM Fornecedor;";

            using var db = _factory.CreateConnection();

            return await db.QueryAsync<Fornecedor>(sql);
        }

        public async Task<int> CreateFornecedor(Fornecedor fornecedor)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@nome", fornecedor.Nome);
            const string sql = @"INSERT INTO Fornecedor (Nome)
                           VALUES (@nome);
                           SELECT LAST_INSERT_ID();";

            using var db = _factory.CreateConnection();
            return await db.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<int> UpdateFornecedor(Fornecedor fornecedor)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@id", fornecedor.Id);
            parameters.Add("@nome", fornecedor.Nome);
            string sql = @"UPDATE Fornecedor
                           SET Nome = @nome
                           WHERE Id = @id;";
            using var db = _factory.CreateConnection();
            return await db.ExecuteAsync(sql, parameters);
        }

        public async Task<bool> DeleteFornecedor(int fornecedorId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@id", fornecedorId);
            string sql = @"DELETE FROM Fornecedor
                           WHERE Id = @id;";
            using var db = _factory.CreateConnection();
            var affectedRows = await db.ExecuteAsync(sql, parameters);
            return affectedRows > 0;
        }
    }
}
