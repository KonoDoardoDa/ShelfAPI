using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ShelfApi.Infrastructure;
using ShelfApi.Models;

namespace ShelfApi.Repositories;

public class EstoqueKitRepository : IEstoqueKitRepository
{
    private readonly IDbConnectionFactory _factory;

    public EstoqueKitRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<KitSaldoDto>> GetAllSaldosAsync()
    {
        using var db = _factory.CreateConnection();

        // 1. Obter todos os kits e seus saldos físicos atuais
        const string sqlKits = @"
            SELECT 
                k.Id AS KitId,
                k.Nome AS NomeKit,
                COALESCE(ek.quantidade, 0) AS SaldoFisico
            FROM kit k
            LEFT JOIN estoque_kit ek ON k.Id = ek.kitId
            ORDER BY k.Nome ASC;";

        var kits = (await db.QueryAsync<KitSaldoDto>(sqlKits)).ToList();

        // 2. Obter todos os itens de kit com seus saldos de estoque atuais para calcular capacidade BOM
        const string sqlItens = @"
            SELECT 
                ki.KitId,
                ki.ItemId,
                i.Nome AS NomeItem,
                i.FornecedorId,
                COALESCE(f.Nome, 'Sem Fornecedor') AS NomeFornecedor,
                ki.Quantidade AS QuantidadeNecessaria,
                ki.IsPainel,
                COALESCE(e.quantidade, 0) AS SaldoEstoqueItem
            FROM kititem ki
            INNER JOIN item i ON ki.ItemId = i.Id
            LEFT JOIN fornecedor f ON i.FornecedorId = f.Id
            LEFT JOIN estoque e ON ki.ItemId = e.itemId;";

        var itensPorKit = (await db.QueryAsync<dynamic>(sqlItens))
            .GroupBy(x => (int)x.KitId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var kit in kits)
        {
            if (itensPorKit.TryGetValue(kit.KitId, out var itens))
            {
                kit.TotalItensComposicao = itens.Count;
                kit.Itens = itens.Select(it => new KitItemDisponibilidadeDto
                {
                    ItemId = (int)it.ItemId,
                    NomeItem = (string)it.NomeItem,
                    FornecedorId = it.FornecedorId != null ? (int?)it.FornecedorId : null,
                    NomeFornecedor = (string)it.NomeFornecedor ?? "Sem Fornecedor",
                    QuantidadeNecessaria = (int)it.QuantidadeNecessaria,
                    SaldoEstoqueItem = (int)it.SaldoEstoqueItem,
                    IsPainel = Convert.ToBoolean(it.IsPainel)
                }).ToList();

                if (kit.Itens.Count > 0)
                {
                    kit.CapacidadeMontagem = kit.Itens.Min(i => i.MontagensPossiveis);
                }
                else
                {
                    kit.CapacidadeMontagem = 0;
                }
            }
            else
            {
                kit.TotalItensComposicao = 0;
                kit.CapacidadeMontagem = 0;
                kit.Itens = new List<KitItemDisponibilidadeDto>();
            }
        }

        return kits;
    }

    public async Task<KitSaldoDto?> GetSaldoByIdAsync(int kitId)
    {
        var saldos = await GetAllSaldosAsync();
        return saldos.FirstOrDefault(k => k.KitId == kitId);
    }

    public async Task<ResultadoOperacaoKitDto> MontarKitAsync(
        int kitId,
        int quantidade,
        string? motivo,
        int? usuarioId,
        string usuarioNome)
    {
        const int maxQuantidadeOperacao = 1_000_000;
        if (quantidade <= 0 || quantidade > maxQuantidadeOperacao)
        {
            throw new ArgumentException($"A quantidade a ser montada deve ser entre 1 e {maxQuantidadeOperacao:N0} unidades.", nameof(quantidade));
        }

        using var db = _factory.CreateConnection();
        if (db.State != ConnectionState.Open)
        {
            db.Open();
        }

        using var tx = db.BeginTransaction();

        try
        {
            // 1. Obter nome do kit
            const string sqlKitNome = "SELECT Nome FROM kit WHERE Id = @kitId;";
            var nomeKit = await db.QueryFirstOrDefaultAsync<string>(sqlKitNome, new { kitId }, tx);
            if (nomeKit == null)
            {
                throw new KeyNotFoundException($"Kit com ID #{kitId} não foi encontrado.");
            }

            // 2. Obter composição do kit e saldos atuais dos itens componentes
            const string sqlItens = @"
                SELECT 
                    ki.ItemId,
                    i.Nome AS NomeItem,
                    ki.Quantidade AS QuantidadeNecessaria,
                    COALESCE(e.quantidade, 0) AS SaldoEstoqueItem
                FROM kititem ki
                INNER JOIN item i ON ki.ItemId = i.Id
                LEFT JOIN estoque e ON ki.ItemId = e.itemId
                WHERE ki.KitId = @kitId;";

            var itens = (await db.QueryAsync<KitItemDisponibilidadeDto>(sqlItens, new { kitId }, tx)).ToList();
            if (itens.Count == 0)
            {
                throw new InvalidOperationException($"O kit '{nomeKit}' não possui componentes cadastrados na sua composição.");
            }

            // 3. Validar se há estoque suficiente para TODOS os componentes
            var itensFaltantes = new List<string>();
            foreach (var it in itens)
            {
                long totalNecessarioLong = (long)it.QuantidadeNecessaria * (long)quantidade;
                if (totalNecessarioLong > int.MaxValue)
                {
                    throw new InvalidOperationException($"A quantidade necessária calculada para '{it.NomeItem}' excede a capacidade suportada.");
                }
                var totalNecessario = (int)totalNecessarioLong;
                if (it.SaldoEstoqueItem < totalNecessario)
                {
                    itensFaltantes.Add($"{it.NomeItem} (Saldo atual: {it.SaldoEstoqueItem} un, Necessário: {totalNecessario} un)");
                }
            }

            if (itensFaltantes.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Saldo insuficiente de componentes para montar {quantidade} un do kit '{nomeKit}': {string.Join("; ", itensFaltantes)}");
            }

            // 4. Efetuar saída das peças avulsas de stock.estoque e registrar na auditoria
            var motivoOperacao = string.IsNullOrWhiteSpace(motivo)
                ? $"Montagem de {quantidade} un do Kit '{nomeKit}'"
                : $"Montagem do Kit '{nomeKit}' ({quantidade} un): {motivo.Trim()}";

            foreach (var it in itens)
            {
                var totalDeduzir = it.QuantidadeNecessaria * quantidade;
                var saldoAnteriorItem = it.SaldoEstoqueItem;
                var saldoPosteriorItem = saldoAnteriorItem - totalDeduzir;

                const string sqlUpdateEstoque = "UPDATE estoque SET quantidade = @saldoPosteriorItem WHERE itemId = @itemId;";
                await db.ExecuteAsync(sqlUpdateEstoque, new { saldoPosteriorItem, itemId = it.ItemId }, tx);

                const string sqlLogEstoque = @"
                    INSERT INTO movimentacao_estoque 
                        (itemId, tipo, quantidade, saldoAnterior, saldoPosterior, motivo, usuarioId, usuarioNome, dataMovimentacao)
                    VALUES 
                        (@itemId, 'Saida', @totalDeduzir, @saldoAnteriorItem, @saldoPosteriorItem, @motivoOperacao, @usuarioId, @usuarioNome, NOW());";

                await db.ExecuteAsync(sqlLogEstoque, new
                {
                    itemId = it.ItemId,
                    totalDeduzir,
                    saldoAnteriorItem,
                    saldoPosteriorItem,
                    motivoOperacao,
                    usuarioId,
                    usuarioNome = string.IsNullOrWhiteSpace(usuarioNome) ? "Sistema" : usuarioNome
                }, tx);
            }

            // 5. Incrementar saldo físico do kit em stock.estoque_kit
            const string sqlSaldoKit = "SELECT quantidade FROM estoque_kit WHERE kitId = @kitId;";
            var saldoAtualKitNullable = await db.QueryFirstOrDefaultAsync<int?>(sqlSaldoKit, new { kitId }, tx);
            var saldoAnteriorKit = saldoAtualKitNullable ?? 0;
            long saldoPosteriorKitLong = (long)saldoAnteriorKit + (long)quantidade;
            const long maxSaldoKit = 100_000_000;
            if (saldoPosteriorKitLong > maxSaldoKit)
            {
                throw new InvalidOperationException($"O saldo resultante de kits excederia o limite máximo permitido ({maxSaldoKit:N0} unidades).");
            }
            var saldoPosteriorKit = (int)saldoPosteriorKitLong;

            if (saldoAtualKitNullable == null)
            {
                const string sqlInsertKit = "INSERT INTO estoque_kit (kitId, quantidade) VALUES (@kitId, @saldoPosteriorKit);";
                await db.ExecuteAsync(sqlInsertKit, new { kitId, saldoPosteriorKit }, tx);
            }
            else
            {
                const string sqlUpdateKit = "UPDATE estoque_kit SET quantidade = @saldoPosteriorKit WHERE kitId = @kitId;";
                await db.ExecuteAsync(sqlUpdateKit, new { kitId, saldoPosteriorKit }, tx);
            }

            // 6. Registrar histórico da movimentação do kit
            const string sqlLogKit = @"
                INSERT INTO movimentacao_kit 
                    (kitId, tipo, quantidade, saldoAnterior, saldoPosterior, motivo, usuarioId, usuarioNome, dataMovimentacao)
                VALUES 
                    (@kitId, 'MONTAGEM', @quantidade, @saldoAnteriorKit, @saldoPosteriorKit, @motivoOperacao, @usuarioId, @usuarioNome, NOW());";

            await db.ExecuteAsync(sqlLogKit, new
            {
                kitId,
                quantidade,
                saldoAnteriorKit,
                saldoPosteriorKit,
                motivoOperacao,
                usuarioId,
                usuarioNome = string.IsNullOrWhiteSpace(usuarioNome) ? "Sistema" : usuarioNome
            }, tx);

            tx.Commit();

            return new ResultadoOperacaoKitDto
            {
                KitId = kitId,
                NomeKit = nomeKit,
                NovoSaldoKit = saldoPosteriorKit,
                Mensagem = $"Montagem de {quantidade} kit(s) '{nomeKit}' concluída com sucesso. Peças deduzidas do estoque."
            };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<ResultadoOperacaoKitDto> DesmontarKitAsync(
        int kitId,
        int quantidade,
        string? motivo,
        int? usuarioId,
        string usuarioNome)
    {
        const int maxQuantidadeOperacao = 1_000_000;
        if (quantidade <= 0 || quantidade > maxQuantidadeOperacao)
        {
            throw new ArgumentException($"A quantidade a ser desmontada deve ser entre 1 e {maxQuantidadeOperacao:N0} unidades.", nameof(quantidade));
        }

        using var db = _factory.CreateConnection();
        if (db.State != ConnectionState.Open)
        {
            db.Open();
        }

        using var tx = db.BeginTransaction();

        try
        {
            // 1. Obter nome e saldo do kit
            const string sqlKit = @"
                SELECT k.Nome AS NomeKit, COALESCE(ek.quantidade, 0) AS SaldoAtual
                FROM kit k
                LEFT JOIN estoque_kit ek ON k.Id = ek.kitId
                WHERE k.Id = @kitId;";

            var kitInfo = await db.QueryFirstOrDefaultAsync<dynamic>(sqlKit, new { kitId }, tx);
            if (kitInfo == null)
            {
                throw new KeyNotFoundException($"Kit com ID #{kitId} não foi encontrado.");
            }

            string nomeKit = kitInfo.NomeKit;
            int saldoAnteriorKit = (int)kitInfo.SaldoAtual;

            if (saldoAnteriorKit < quantidade)
            {
                throw new InvalidOperationException(
                    $"Saldo insuficiente de kits montados para desmontagem. Saldo em estoque: {saldoAnteriorKit} un, solicitado: {quantidade} un.");
            }

            var saldoPosteriorKit = saldoAnteriorKit - quantidade;

            // 2. Obter componentes do kit
            const string sqlItens = @"
                SELECT 
                    ki.ItemId,
                    i.Nome AS NomeItem,
                    ki.Quantidade AS QuantidadeNecessaria,
                    COALESCE(e.quantidade, 0) AS SaldoEstoqueItem
                FROM kititem ki
                INNER JOIN item i ON ki.ItemId = i.Id
                LEFT JOIN estoque e ON ki.ItemId = e.itemId
                WHERE ki.KitId = @kitId;";

            var itens = (await db.QueryAsync<KitItemDisponibilidadeDto>(sqlItens, new { kitId }, tx)).ToList();

            // 3. Atualizar estoque do kit
            const string sqlUpdateKit = "UPDATE estoque_kit SET quantidade = @saldoPosteriorKit WHERE kitId = @kitId;";
            await db.ExecuteAsync(sqlUpdateKit, new { kitId, saldoPosteriorKit }, tx);

            var motivoOperacao = string.IsNullOrWhiteSpace(motivo)
                ? $"Desmontagem de {quantidade} un do Kit '{nomeKit}' (Devolução de componentes)"
                : $"Desmontagem do Kit '{nomeKit}' ({quantidade} un): {motivo.Trim()}";

            // 4. Registrar histórico da movimentação do kit
            const string sqlLogKit = @"
                INSERT INTO movimentacao_kit 
                    (kitId, tipo, quantidade, saldoAnterior, saldoPosterior, motivo, usuarioId, usuarioNome, dataMovimentacao)
                VALUES 
                    (@kitId, 'DESMONTAGEM', @quantidade, @saldoAnteriorKit, @saldoPosteriorKit, @motivoOperacao, @usuarioId, @usuarioNome, NOW());";

            await db.ExecuteAsync(sqlLogKit, new
            {
                kitId,
                quantidade,
                saldoAnteriorKit,
                saldoPosteriorKit,
                motivoOperacao,
                usuarioId,
                usuarioNome = string.IsNullOrWhiteSpace(usuarioNome) ? "Sistema" : usuarioNome
            }, tx);

            // 5. Devolver peças avulsas para o estoque de cada item
            const long maxSaldoEstoque = 100_000_000;
            foreach (var it in itens)
            {
                long totalDevolverLong = (long)it.QuantidadeNecessaria * (long)quantidade;
                if (totalDevolverLong > int.MaxValue)
                {
                    throw new InvalidOperationException($"A quantidade a ser devolvida para '{it.NomeItem}' excede a capacidade do sistema.");
                }
                var totalDevolver = (int)totalDevolverLong;

                var saldoAnteriorItem = it.SaldoEstoqueItem;
                long saldoPosteriorItemLong = (long)saldoAnteriorItem + totalDevolverLong;
                if (saldoPosteriorItemLong > maxSaldoEstoque)
                {
                    throw new InvalidOperationException($"A devolução de peças resultaria em um saldo de {saldoPosteriorItemLong:N0} unidades para '{it.NomeItem}', excedendo o limite máximo ({maxSaldoEstoque:N0}).");
                }
                var saldoPosteriorItem = (int)saldoPosteriorItemLong;

                // Se item já existe em estoque atualiza, senão insere
                const string sqlCheckItem = "SELECT COUNT(1) FROM estoque WHERE itemId = @itemId;";
                var existe = await db.ExecuteScalarAsync<int>(sqlCheckItem, new { itemId = it.ItemId }, tx) > 0;

                if (existe)
                {
                    const string sqlUpdateEstoque = "UPDATE estoque SET quantidade = @saldoPosteriorItem WHERE itemId = @itemId;";
                    await db.ExecuteAsync(sqlUpdateEstoque, new { saldoPosteriorItem, itemId = it.ItemId }, tx);
                }
                else
                {
                    const string sqlInsertEstoque = "INSERT INTO estoque (itemId, quantidade) VALUES (@itemId, @saldoPosteriorItem);";
                    await db.ExecuteAsync(sqlInsertEstoque, new { itemId = it.ItemId, saldoPosteriorItem }, tx);
                }

                const string sqlLogEstoque = @"
                    INSERT INTO movimentacao_estoque 
                        (itemId, tipo, quantidade, saldoAnterior, saldoPosterior, motivo, usuarioId, usuarioNome, dataMovimentacao)
                    VALUES 
                        (@itemId, 'Entrada', @totalDevolver, @saldoAnteriorItem, @saldoPosteriorItem, @motivoOperacao, @usuarioId, @usuarioNome, NOW());";

                await db.ExecuteAsync(sqlLogEstoque, new
                {
                    itemId = it.ItemId,
                    totalDevolver,
                    saldoAnteriorItem,
                    saldoPosteriorItem,
                    motivoOperacao,
                    usuarioId,
                    usuarioNome = string.IsNullOrWhiteSpace(usuarioNome) ? "Sistema" : usuarioNome
                }, tx);
            }

            tx.Commit();

            return new ResultadoOperacaoKitDto
            {
                KitId = kitId,
                NomeKit = nomeKit,
                NovoSaldoKit = saldoPosteriorKit,
                Mensagem = $"Desmontagem de {quantidade} kit(s) '{nomeKit}' concluída com sucesso. Componentes retornados ao estoque."
            };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<ResultadoOperacaoKitDto> MovimentarManualAsync(
        int kitId,
        string tipo,
        int quantidade,
        string? motivo,
        int? usuarioId,
        string usuarioNome)
    {
        const int maxQuantidadeOperacao = 1_000_000;
        if (quantidade <= 0 || quantidade > maxQuantidadeOperacao)
        {
            throw new ArgumentException($"A quantidade deve ser entre 1 e {maxQuantidadeOperacao:N0} unidades.", nameof(quantidade));
        }

        var isEntrada = tipo.Equals("Entrada", StringComparison.OrdinalIgnoreCase);

        using var db = _factory.CreateConnection();
        if (db.State != ConnectionState.Open)
        {
            db.Open();
        }

        using var tx = db.BeginTransaction();

        try
        {
            const string sqlKit = @"
                SELECT k.Nome AS NomeKit, COALESCE(ek.quantidade, 0) AS SaldoAtual, ek.kitId AS HasRegistro
                FROM kit k
                LEFT JOIN estoque_kit ek ON k.Id = ek.kitId
                WHERE k.Id = @kitId;";

            var kitInfo = await db.QueryFirstOrDefaultAsync<dynamic>(sqlKit, new { kitId }, tx);
            if (kitInfo == null)
            {
                throw new KeyNotFoundException($"Kit com ID #{kitId} não foi encontrado.");
            }

            string nomeKit = kitInfo.NomeKit;
            int saldoAnterior = (int)kitInfo.SaldoAtual;
            bool hasRegistro = kitInfo.HasRegistro != null;

            long saldoPosteriorLong = isEntrada ? (long)saldoAnterior + (long)quantidade : (long)saldoAnterior - (long)quantidade;

            if (saldoPosteriorLong < 0)
            {
                throw new InvalidOperationException(
                    $"Saldo insuficiente de kits em estoque. Saldo atual: {saldoAnterior} un, solicitado: {quantidade} un.");
            }

            const long maxSaldoKit = 100_000_000;
            if (saldoPosteriorLong > maxSaldoKit)
            {
                throw new InvalidOperationException($"O saldo resultante de kits excederia o limite máximo permitido ({maxSaldoKit:N0} unidades).");
            }

            int saldoPosterior = (int)saldoPosteriorLong;

            if (!hasRegistro)
            {
                const string sqlInsert = "INSERT INTO estoque_kit (kitId, quantidade) VALUES (@kitId, @saldoPosterior);";
                await db.ExecuteAsync(sqlInsert, new { kitId, saldoPosterior }, tx);
            }
            else
            {
                const string sqlUpdate = "UPDATE estoque_kit SET quantidade = @saldoPosterior WHERE kitId = @kitId;";
                await db.ExecuteAsync(sqlUpdate, new { kitId, saldoPosterior }, tx);
            }

            var tipoFormatado = isEntrada ? "ENTRADA" : "SAIDA";
            var motivoFormatado = string.IsNullOrWhiteSpace(motivo)
                ? $"Movimentação manual ({tipoFormatado})"
                : motivo.Trim();

            const string sqlLog = @"
                INSERT INTO movimentacao_kit 
                    (kitId, tipo, quantidade, saldoAnterior, saldoPosterior, motivo, usuarioId, usuarioNome, dataMovimentacao)
                VALUES 
                    (@kitId, @tipoFormatado, @quantidade, @saldoAnterior, @saldoPosterior, @motivoFormatado, @usuarioId, @usuarioNome, NOW());";

            await db.ExecuteAsync(sqlLog, new
            {
                kitId,
                tipoFormatado,
                quantidade,
                saldoAnterior,
                saldoPosterior,
                motivoFormatado,
                usuarioId,
                usuarioNome = string.IsNullOrWhiteSpace(usuarioNome) ? "Sistema" : usuarioNome
            }, tx);

            tx.Commit();

            return new ResultadoOperacaoKitDto
            {
                KitId = kitId,
                NomeKit = nomeKit,
                NovoSaldoKit = saldoPosterior,
                Mensagem = $"Movimentação ({tipoFormatado}) de {quantidade} kit(s) registrada com sucesso. Novo saldo: {saldoPosterior} un."
            };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<MovimentacaoKitDetalheDto>> GetHistoricoAsync(int? kitId = null, int limit = 100)
    {
        const string sql = @"
            SELECT 
                mk.id AS Id,
                mk.kitId AS KitId,
                k.Nome AS NomeKit,
                mk.tipo AS Tipo,
                mk.quantidade AS Quantidade,
                mk.saldoAnterior AS SaldoAnterior,
                mk.saldoPosterior AS SaldoPosterior,
                mk.motivo AS Motivo,
                mk.usuarioId AS UsuarioId,
                mk.usuarioNome AS UsuarioNome,
                mk.dataMovimentacao AS DataMovimentacao
            FROM movimentacao_kit mk
            INNER JOIN kit k ON mk.kitId = k.Id
            WHERE (@kitId IS NULL OR mk.kitId = @kitId)
            ORDER BY mk.dataMovimentacao DESC, mk.id DESC
            LIMIT @limit;";

        using var db = _factory.CreateConnection();
        return await db.QueryAsync<MovimentacaoKitDetalheDto>(sql, new { kitId, limit });
    }

    public async Task<SimulacaoProducaoResultadoDto> SimularProducaoAsync(IEnumerable<ItemSimulacaoInputDto> itensSimulacao)
    {
        var inputList = (itensSimulacao ?? Enumerable.Empty<ItemSimulacaoInputDto>())
            .Where(x => x.KitId > 0 && x.Quantidade > 0)
            .ToList();

        var resultado = new SimulacaoProducaoResultadoDto
        {
            TotalKitsSolicitados = inputList.Sum(x => x.Quantidade),
            TotalTiposKits = inputList.Select(x => x.KitId).Distinct().Count()
        };

        if (inputList.Count == 0)
        {
            resultado.ProducaoTotalmenteViavel = true;
            return resultado;
        }

        var kitIds = inputList.Select(x => x.KitId).Distinct().ToList();

        const string sqlKitsEItens = @"
            SELECT 
                k.Id AS KitId,
                k.Nome AS NomeKit,
                ki.ItemId,
                i.Nome AS NomeItem,
                i.FornecedorId,
                COALESCE(f.Nome, 'Sem Fornecedor') AS NomeFornecedor,
                ki.Quantidade AS QuantidadePorKit,
                ki.IsPainel,
                COALESCE(e.quantidade, 0) AS SaldoEstoqueItem
            FROM kit k
            INNER JOIN kititem ki ON k.Id = ki.KitId
            INNER JOIN item i ON ki.ItemId = i.Id
            LEFT JOIN fornecedor f ON i.FornecedorId = f.Id
            LEFT JOIN estoque e ON ki.ItemId = e.itemId
            WHERE k.Id IN @kitIds;";

        using var db = _factory.CreateConnection();
        var rawRows = (await db.QueryAsync<dynamic>(sqlKitsEItens, new { kitIds })).ToList();

        var itemMap = new Dictionary<int, ItemSimulacaoResultadoDto>();

        foreach (var input in inputList)
        {
            var rowsDoKit = rawRows.Where(r => (int)r.KitId == input.KitId).ToList();
            foreach (var row in rowsDoKit)
            {
                int itemId = (int)row.ItemId;
                string nomeItem = (string)row.NomeItem;
                int? fornecedorId = row.FornecedorId != null ? (int?)row.FornecedorId : null;
                string nomeFornecedor = (string)row.NomeFornecedor ?? "Sem Fornecedor";
                int qtdPorKit = (int)row.QuantidadePorKit;
                bool isPainel = Convert.ToBoolean(row.IsPainel);
                int saldoEstoque = (int)row.SaldoEstoqueItem;
                string nomeKit = (string)row.NomeKit;
                int subtotal = qtdPorKit * input.Quantidade;

                if (!itemMap.TryGetValue(itemId, out var itemResult))
                {
                    itemResult = new ItemSimulacaoResultadoDto
                    {
                        ItemId = itemId,
                        NomeItem = nomeItem,
                        FornecedorId = fornecedorId,
                        NomeFornecedor = nomeFornecedor,
                        SaldoEstoqueItem = saldoEstoque,
                        IsPainel = isPainel,
                        QuantidadeTotalNecessaria = 0,
                        KitsOrigem = new List<ItemSimulacaoKitOrigemDto>()
                    };
                    itemMap[itemId] = itemResult;
                }
                else if (isPainel)
                {
                    itemResult.IsPainel = true;
                }

                itemResult.QuantidadeTotalNecessaria += subtotal;
                itemResult.KitsOrigem.Add(new ItemSimulacaoKitOrigemDto
                {
                    KitId = input.KitId,
                    NomeKit = nomeKit,
                    QuantidadeKit = input.Quantidade,
                    QuantidadePorKit = qtdPorKit,
                    SubtotalNecessario = subtotal,
                    IsPainel = isPainel
                });
            }
        }

        foreach (var itemResult in itemMap.Values)
        {
            itemResult.SaldoAposMontagem = itemResult.SaldoEstoqueItem - itemResult.QuantidadeTotalNecessaria;
            itemResult.FaltaEstoque = Math.Max(0, itemResult.QuantidadeTotalNecessaria - itemResult.SaldoEstoqueItem);
            itemResult.Suficiente = itemResult.SaldoEstoqueItem >= itemResult.QuantidadeTotalNecessaria;
        }

        var listaOrdenada = itemMap.Values
            .OrderBy(i => i.Suficiente ? 1 : 0)
            .ThenBy(i => i.NomeItem)
            .ToList();

        resultado.Itens = listaOrdenada;
        resultado.TotalComponentesDistintos = listaOrdenada.Count;
        resultado.TotalComponentesAtendidos = listaOrdenada.Count(i => i.Suficiente);
        resultado.TotalComponentesFaltantes = listaOrdenada.Count(i => !i.Suficiente);
        resultado.ProducaoTotalmenteViavel = resultado.TotalComponentesFaltantes == 0;

        return resultado;
    }
}
