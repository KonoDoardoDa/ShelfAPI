using Dapper;
using ShelfApi.Models;
using System.Data;

namespace ShelfApi.Repositories
{
    public class FornecedorRepository : IFornecedorRepository
    {
        private readonly IDbConnection _db;

        public FornecedorRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Fornecedor>> GetAllAsync()
        {
            var sql = "SELECT Id, Nome FROM Fornecedor;";

            return await _db.QueryAsync<Fornecedor>(sql);

        }

        public async Task<int> CreateFornecedor(Fornecedor fornecedor)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@nome", fornecedor.Nome);
            string sql = @"INSERT INTO Fornecedor (Nome)
                           VALUES (@nome);
                           SELECT LAST_INSERT_ID();";
            return await _db.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<int> UpdateFornecedor(Fornecedor fornecedor)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@id", fornecedor.Id);
            parameters.Add("@nome", fornecedor.Nome);
            string sql = @"UPDATE Fornecedor
                           SET Nome = @nome
                           WHERE Id = @id;";
            var affectedRows = await _db.ExecuteAsync(sql, parameters);
            return affectedRows;
        }

        public async Task<int> DeleteFornecedor(int fornecedorId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@id", fornecedorId);
            string sql = @"DELETE FROM Fornecedor
                           WHERE Id = @id;";
            var affectedRows = await _db.ExecuteAsync(sql, parameters);
            return affectedRows;
        }
    }
}
