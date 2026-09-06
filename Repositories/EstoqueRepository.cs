using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using ShelfApi.Infrastructure;
using ShelfApi.Models;

namespace ShelfApi.Repositories;

public class EstoqueRepository : IEstoqueRepository
{
    private readonly IDbConnectionFactory _factory;

    public EstoqueRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<int> GetSaldoByItemIdAsync(int itemId)
    {
        const string sql = "SELECT COALESCE(quantidade, 0) FROM estoque WHERE itemId = @itemId;";
        using var db = _factory.CreateConnection();
        return await db.ExecuteScalarAsync<int>(sql, new { itemId });
    }

    public async Task<IEnumerable<EstoqueSaldoDto>> GetAllSaldosAsync()
    {
        const string sql = @"
            SELECT 
                i.Id AS ItemId, 
                i.Nome AS NomeItem, 
                COALESCE(e.quantidade, 0) AS Quantidade 
            FROM Item i
            LEFT JOIN estoque e ON i.Id = e.itemId;";
        using var db = _factory.CreateConnection();
        return await db.QueryAsync<EstoqueSaldoDto>(sql);
    }

    public async Task<int> MovimentarAsync(
        int itemId,
        int delta,
        string tipo,
        int quantidade,
        string? motivo,
        int? usuarioId,
        string usuarioNome)
    {
        using var db = _factory.CreateConnection();

        // 1. Obter saldo anterior
        const string sqlSelect = "SELECT quantidade FROM estoque WHERE itemId = @itemId;";
        var saldoAtual = await db.QueryFirstOrDefaultAsync<int?>(sqlSelect, new { itemId });
        var saldoAnterior = saldoAtual ?? 0;

        if (saldoAnterior + delta < 0)
        {
            throw new InvalidOperationException(
                $"Saldo insuficiente em estoque. Saldo atual: {saldoAnterior}, quantidade solicitada para saída: {Math.Abs(delta)}.");
        }

        var saldoPosterior = saldoAnterior + delta;

        // 2. Atualizar ou inserir saldo na tabela estoque
        if (saldoAtual == null)
        {
            const string sqlInsert = "INSERT INTO estoque (itemId, quantidade) VALUES (@itemId, @quantidade);";
            await db.ExecuteAsync(sqlInsert, new { itemId, quantidade = saldoPosterior });
        }
        else
        {
            const string sqlUpdate = "UPDATE estoque SET quantidade = @quantidade WHERE itemId = @itemId;";
            await db.ExecuteAsync(sqlUpdate, new { itemId, quantidade = saldoPosterior });
        }

        // 3. Gravar log de auditoria na tabela movimentacao_estoque
        const string sqlLog = @"
            INSERT INTO movimentacao_estoque 
                (itemId, tipo, quantidade, saldoAnterior, saldoPosterior, motivo, usuarioId, usuarioNome, dataMovimentacao)
            VALUES 
                (@itemId, @tipo, @quantidade, @saldoAnterior, @saldoPosterior, @motivo, @usuarioId, @usuarioNome, NOW());";

        await db.ExecuteAsync(sqlLog, new
        {
            itemId,
            tipo,
            quantidade,
            saldoAnterior,
            saldoPosterior,
            motivo,
            usuarioId,
            usuarioNome = string.IsNullOrWhiteSpace(usuarioNome) ? "Sistema" : usuarioNome
        });

        return saldoPosterior;
    }

    public async Task<IEnumerable<MovimentacaoEstoqueDetalheDto>> GetHistoricoAsync(int? itemId = null, int limit = 100)
    {
        const string sql = @"
            SELECT 
                m.id AS Id,
                m.itemId AS ItemId,
                COALESCE(i.Nome, 'Item Removido') AS NomeItem,
                m.tipo AS Tipo,
                m.quantidade AS Quantidade,
                m.saldoAnterior AS SaldoAnterior,
                m.saldoPosterior AS SaldoPosterior,
                m.motivo AS Motivo,
                m.usuarioId AS UsuarioId,
                m.usuarioNome AS UsuarioNome,
                m.dataMovimentacao AS DataMovimentacao
            FROM movimentacao_estoque m
            LEFT JOIN Item i ON m.itemId = i.Id
            WHERE (@itemId IS NULL OR m.itemId = @itemId)
            ORDER BY m.dataMovimentacao DESC, m.id DESC
            LIMIT @limit;";

        using var db = _factory.CreateConnection();
        return await db.QueryAsync<MovimentacaoEstoqueDetalheDto>(sql, new { itemId, limit });
    }
}
