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
        string usuarioNome,
        string? notaFiscal = null)
    {
        using var db = _factory.CreateConnection();

        // 1. Obter saldo anterior
        const string sqlSelect = "SELECT quantidade FROM estoque WHERE itemId = @itemId;";
        var saldoAtual = await db.QueryFirstOrDefaultAsync<int?>(sqlSelect, new { itemId });
        var saldoAnterior = saldoAtual ?? 0;

        long saldoFuturo = (long)saldoAnterior + delta;

        if (saldoFuturo < 0)
        {
            throw new InvalidOperationException(
                $"Saldo insuficiente em estoque. Saldo atual: {saldoAnterior}, quantidade solicitada para saída: {Math.Abs(delta)}.");
        }

        const long maxSaldoEstoque = 100_000_000;
        if (saldoFuturo > maxSaldoEstoque)
        {
            throw new InvalidOperationException(
                $"A operação resultaria em um saldo de {saldoFuturo:N0} unidades, excedendo o limite máximo permitido de estoque ({maxSaldoEstoque:N0}).");
        }

        var saldoPosterior = (int)saldoFuturo;

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
                (itemId, tipo, quantidade, saldoAnterior, saldoPosterior, motivo, notaFiscal, usuarioId, usuarioNome, dataMovimentacao)
            VALUES 
                (@itemId, @tipo, @quantidade, @saldoAnterior, @saldoPosterior, @motivo, @notaFiscal, @usuarioId, @usuarioNome, NOW());";

        await db.ExecuteAsync(sqlLog, new
        {
            itemId,
            tipo,
            quantidade,
            saldoAnterior,
            saldoPosterior,
            motivo,
            notaFiscal = string.IsNullOrWhiteSpace(notaFiscal) ? null : notaFiscal.Trim(),
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
                COALESCE(f.Nome, 'Sem Fornecedor') AS FornecedorNome,
                m.tipo AS Tipo,
                m.quantidade AS Quantidade,
                m.saldoAnterior AS SaldoAnterior,
                m.saldoPosterior AS SaldoPosterior,
                m.motivo AS Motivo,
                m.notaFiscal AS NotaFiscal,
                m.usuarioId AS UsuarioId,
                m.usuarioNome AS UsuarioNome,
                m.dataMovimentacao AS DataMovimentacao
            FROM movimentacao_estoque m
            LEFT JOIN Item i ON m.itemId = i.Id
            LEFT JOIN Fornecedor f ON i.FornecedorId = f.Id
            WHERE (@itemId IS NULL OR m.itemId = @itemId)
            ORDER BY m.dataMovimentacao DESC, m.id DESC
            LIMIT @limit;";

        using var db = _factory.CreateConnection();
        return await db.QueryAsync<MovimentacaoEstoqueDetalheDto>(sql, new { itemId, limit });
    }

    public async Task<ImportarLoteResultadoDto> ImportarLoteAsync(ImportarLoteRequestDto dto, int? usuarioId = null, string? usuarioNome = null)
    {
        if (dto.Itens == null || dto.Itens.Count == 0)
        {
            throw new ArgumentException("Nenhum item fornecido para importação.");
        }

        var resultado = new ImportarLoteResultadoDto();
        var operador = !string.IsNullOrWhiteSpace(usuarioNome)
            ? usuarioNome
            : (!string.IsNullOrWhiteSpace(dto.UsuarioNome) ? dto.UsuarioNome : "Sistema");
        var nfFormatada = string.IsNullOrWhiteSpace(dto.NotaFiscal) ? null : dto.NotaFiscal.Trim();

        using var db = _factory.CreateConnection();
        if (db.State != System.Data.ConnectionState.Open)
        {
            db.Open();
        }

        using var transaction = db.BeginTransaction();

        try
        {
            // 1. Determinar o fornecedor padrão
            int fornecedorIdFinal = 0;
            string fornecedorNomeFinal = "Fornecedor Geral";

            if (dto.FornecedorIdPadrao.HasValue && dto.FornecedorIdPadrao.Value > 0)
            {
                fornecedorIdFinal = dto.FornecedorIdPadrao.Value;
                const string sqlForn = "SELECT Nome FROM Fornecedor WHERE Id = @fornecedorIdFinal;";
                fornecedorNomeFinal = await db.QueryFirstOrDefaultAsync<string>(sqlForn, new { fornecedorIdFinal }, transaction) ?? "Fornecedor";
            }
            else if (!string.IsNullOrWhiteSpace(dto.NovoFornecedorNome))
            {
                var nomeNovo = dto.NovoFornecedorNome.Trim();
                const string sqlBuscaForn = "SELECT Id FROM Fornecedor WHERE LOWER(Nome) = LOWER(@nomeNovo) LIMIT 1;";
                var idExistente = await db.QueryFirstOrDefaultAsync<int?>(sqlBuscaForn, new { nomeNovo }, transaction);

                if (idExistente.HasValue && idExistente.Value > 0)
                {
                    fornecedorIdFinal = idExistente.Value;
                    fornecedorNomeFinal = nomeNovo;
                }
                else
                {
                    const string sqlInsForn = "INSERT INTO Fornecedor (Nome, Ativo) VALUES (@nomeNovo, 1); SELECT LAST_INSERT_ID();";
                    fornecedorIdFinal = await db.ExecuteScalarAsync<int>(sqlInsForn, new { nomeNovo }, transaction);
                    fornecedorNomeFinal = nomeNovo;
                }
            }
            else
            {
                // Busca o primeiro fornecedor ativo ou cria um fornecedor padrão
                const string sqlPrimeiroForn = "SELECT Id, Nome FROM Fornecedor WHERE Ativo = 1 ORDER BY Id ASC LIMIT 1;";
                var fPadrao = await db.QueryFirstOrDefaultAsync<dynamic>(sqlPrimeiroForn, transaction: transaction);
                if (fPadrao != null)
                {
                    fornecedorIdFinal = (int)fPadrao.Id;
                    fornecedorNomeFinal = (string)fPadrao.Nome;
                }
                else
                {
                    const string sqlInsPadrao = "INSERT INTO Fornecedor (Nome, Ativo) VALUES ('Fornecedor Geral', 1); SELECT LAST_INSERT_ID();";
                    fornecedorIdFinal = await db.ExecuteScalarAsync<int>(sqlInsPadrao, transaction: transaction);
                    fornecedorNomeFinal = "Fornecedor Geral";
                }
            }

            resultado.FornecedorId = fornecedorIdFinal;
            resultado.FornecedorNome = fornecedorNomeFinal;

            // 2. Processar cada item do lote
            foreach (var itemDto in dto.Itens)
            {
                if (string.IsNullOrWhiteSpace(itemDto.Nome)) continue;
                var nomeItem = itemDto.Nome.Trim();
                var qtd = itemDto.Quantidade > 0 ? itemDto.Quantidade : 1;
                var fIdItem = (itemDto.FornecedorId.HasValue && itemDto.FornecedorId.Value > 0) ? itemDto.FornecedorId.Value : fornecedorIdFinal;

                // Verificar se o item já existe pelo Nome
                const string sqlBuscaItem = "SELECT Id, Nome FROM Item WHERE LOWER(Nome) = LOWER(@nomeItem) LIMIT 1;";
                var itemExistente = await db.QueryFirstOrDefaultAsync<dynamic>(sqlBuscaItem, new { nomeItem }, transaction);

                int itemIdFinal;
                int saldoAnterior = 0;
                int saldoPosterior = 0;
                bool isNovo = false;

                if (itemExistente != null)
                {
                    itemIdFinal = (int)itemExistente.Id;

                    // Obter saldo anterior
                    const string sqlSaldo = "SELECT quantidade FROM estoque WHERE itemId = @itemIdFinal;";
                    var saldoAtual = await db.QueryFirstOrDefaultAsync<int?>(sqlSaldo, new { itemIdFinal }, transaction);
                    saldoAnterior = saldoAtual ?? 0;
                    saldoPosterior = saldoAnterior + qtd;

                    if (saldoAtual == null)
                    {
                        const string sqlInsEstoque = "INSERT INTO estoque (itemId, quantidade) VALUES (@itemIdFinal, @saldoPosterior);";
                        await db.ExecuteAsync(sqlInsEstoque, new { itemIdFinal, saldoPosterior }, transaction);
                    }
                    else
                    {
                        const string sqlUpdEstoque = "UPDATE estoque SET quantidade = @saldoPosterior WHERE itemId = @itemIdFinal;";
                        await db.ExecuteAsync(sqlUpdEstoque, new { itemIdFinal, saldoPosterior }, transaction);
                    }

                    resultado.ItensAtualizados++;
                }
                else
                {
                    isNovo = true;
                    var desc = !string.IsNullOrWhiteSpace(itemDto.Descricao) 
                        ? itemDto.Descricao.Trim() 
                        : (!string.IsNullOrWhiteSpace(itemDto.CodigoReferencia) ? $"Ref: {itemDto.CodigoReferencia.Trim()}" : null);

                    const string sqlInsItem = @"
                        INSERT INTO Item (Nome, FornecedorId, Descricao, Ativo) 
                        VALUES (@nomeItem, @fIdItem, @desc, 1); 
                        SELECT LAST_INSERT_ID();";
                    itemIdFinal = await db.ExecuteScalarAsync<int>(sqlInsItem, new { nomeItem, fIdItem, desc }, transaction);

                    saldoAnterior = 0;
                    saldoPosterior = qtd;

                    const string sqlInsEstoque = "INSERT INTO estoque (itemId, quantidade) VALUES (@itemIdFinal, @saldoPosterior);";
                    await db.ExecuteAsync(sqlInsEstoque, new { itemIdFinal, saldoPosterior }, transaction);

                    resultado.ItensCriados++;
                }

                // Gravar log de auditoria
                var rotuloNf = nfFormatada != null
                    ? (nfFormatada.StartsWith("NF", StringComparison.OrdinalIgnoreCase) ? $" ({nfFormatada})" : $" (NF {nfFormatada})")
                    : "";

                var motivo = isNovo
                    ? $"Cadastro inicial via importação XML{rotuloNf}"
                    : $"Entrada de estoque via importação XML{rotuloNf}";

                const string sqlLog = @"
                    INSERT INTO movimentacao_estoque 
                        (itemId, tipo, quantidade, saldoAnterior, saldoPosterior, motivo, notaFiscal, usuarioId, usuarioNome, dataMovimentacao)
                    VALUES 
                        (@itemIdFinal, 'Entrada', @qtd, @saldoAnterior, @saldoPosterior, @motivo, @nfFormatada, @usuarioId, @operador, NOW());";

                await db.ExecuteAsync(sqlLog, new
                {
                    itemIdFinal,
                    qtd,
                    saldoAnterior,
                    saldoPosterior,
                    motivo,
                    nfFormatada,
                    usuarioId,
                    operador
                }, transaction);

                resultado.TotalQuantidadeMovimentada += qtd;
            }

            transaction.Commit();

            resultado.Message = $"Importação XML concluída: {resultado.ItensCriados} novo(s) item(ns) cadastrado(s), {resultado.ItensAtualizados} item(ns) com estoque incrementado. Total de peças movimentadas: {resultado.TotalQuantidadeMovimentada} un.";
            return resultado;
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }
}
