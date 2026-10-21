using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShelfApi.Models;

public class ImportarItemLoteDto
{
    [Required(ErrorMessage = "O nome do item é obrigatório.")]
    [StringLength(150, ErrorMessage = "O nome do item não pode exceder 150 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "O código de referência não pode exceder 100 caracteres.")]
    public string? CodigoReferencia { get; set; }

    public int? FornecedorId { get; set; }

    [Range(1, 1_000_000, ErrorMessage = "A quantidade deve ser entre 1 e 1.000.000 unidades.")]
    public int Quantidade { get; set; } = 1;

    [StringLength(500, ErrorMessage = "A descrição não pode exceder 500 caracteres.")]
    public string? Descricao { get; set; }
}

public class ImportarLoteRequestDto
{
    [StringLength(100, ErrorMessage = "A Nota Fiscal não pode exceder 100 caracteres.")]
    public string? NotaFiscal { get; set; }

    public int? FornecedorIdPadrao { get; set; }

    [StringLength(150, ErrorMessage = "O nome do fornecedor não pode exceder 150 caracteres.")]
    public string? NovoFornecedorNome { get; set; }

    public string? UsuarioNome { get; set; }

    [Required(ErrorMessage = "A lista de itens não pode estar vazia.")]
    public List<ImportarItemLoteDto> Itens { get; set; } = new();
}

public class ImportarLoteResultadoDto
{
    public int ItensCriados { get; set; }
    public int ItensAtualizados { get; set; }
    public int TotalQuantidadeMovimentada { get; set; }
    public int? FornecedorId { get; set; }
    public string FornecedorNome { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
