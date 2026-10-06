using System.ComponentModel.DataAnnotations;

namespace StrockWay.DTOs;

// Dados RECEBIDOS pela API (corpo JSON). Os nomes chegam em snake_case:
// ValorUnitario <-> "valor_unitario", PesoKg <-> "peso_kg" etc.
// Aqui só se verifica a presença dos campos; as regras ficam nos Models.

public class EnderecoRequest
{
    [Required(ErrorMessage = "o campo 'corredor' do endereço é obrigatório")]
    public string? Corredor { get; set; }

    [Required(ErrorMessage = "o campo 'prateleira' do endereço é obrigatório")]
    public string? Prateleira { get; set; }
}

public class EnderecoAtualizarRequest
{
    public string? Corredor { get; set; }
    public string? Prateleira { get; set; }
}

public class ProdutoCriarRequest
{
    [Required(ErrorMessage = "o campo 'codigo' é obrigatório")]
    public string? Codigo { get; set; }

    [Required(ErrorMessage = "o campo 'nome' é obrigatório")]
    public string? Nome { get; set; }

    /// <summary>Saldo inicial (opcional, padrão 0).</summary>
    public int? Quantidade { get; set; }

    [Required(ErrorMessage = "o campo 'valor_unitario' é obrigatório")]
    public decimal? ValorUnitario { get; set; }

    [Required(ErrorMessage = "o campo 'peso_kg' é obrigatório")]
    public decimal? PesoKg { get; set; }

    /// <summary>AAAA-MM-DD. Omita para produto não perecível.</summary>
    public DateOnly? Validade { get; set; }

    [Required(ErrorMessage = "o campo 'endereco' é obrigatório: { \"corredor\": \"A\", \"prateleira\": \"01\" }")]
    public EnderecoRequest? Endereco { get; set; }

    [Required(ErrorMessage = "o campo 'estoque_minimo' é obrigatório")]
    public int? EstoqueMinimo { get; set; }

    [Required(ErrorMessage = "o campo 'estoque_maximo' é obrigatório")]
    public int? EstoqueMaximo { get; set; }

    [Required(ErrorMessage = "o campo 'responsavel' é obrigatório")]
    public string? Responsavel { get; set; }
}

/// <summary>Atualização parcial: envie só os campos que mudaram.</summary>
public class ProdutoAtualizarRequest
{
    /// <summary>Não pode ser alterado; se enviado, precisa ser igual ao da URL.</summary>
    public string? Codigo { get; set; }

    /// <summary>Não é aceito: a quantidade só muda por movimentação.</summary>
    public int? Quantidade { get; set; }

    public string? Nome { get; set; }
    public decimal? ValorUnitario { get; set; }
    public decimal? PesoKg { get; set; }
    public DateOnly? Validade { get; set; }

    /// <summary>true = remove a validade (produto passa a ser não perecível).</summary>
    public bool? RemoverValidade { get; set; }

    public EnderecoAtualizarRequest? Endereco { get; set; }
    public int? EstoqueMinimo { get; set; }
    public int? EstoqueMaximo { get; set; }

    [Required(ErrorMessage = "o campo 'responsavel' é obrigatório")]
    public string? Responsavel { get; set; }
}

public class RemoverProdutoRequest
{
    public string? Responsavel { get; set; }
    public string? Motivo { get; set; }
}

public class EntradaRequest
{
    [Required(ErrorMessage = "o campo 'codigo' é obrigatório")]
    public string? Codigo { get; set; }

    [Required(ErrorMessage = "o campo 'quantidade' é obrigatório")]
    public int? Quantidade { get; set; }

    /// <summary>Opcional: custo desta compra. Recalcula o custo médio do produto.</summary>
    public decimal? ValorUnitario { get; set; }

    [Required(ErrorMessage = "o campo 'responsavel' é obrigatório")]
    public string? Responsavel { get; set; }

    public string? Observacao { get; set; }
}

public class SaidaRequest
{
    [Required(ErrorMessage = "o campo 'codigo' é obrigatório")]
    public string? Codigo { get; set; }

    [Required(ErrorMessage = "o campo 'quantidade' é obrigatório")]
    public int? Quantidade { get; set; }

    [Required(ErrorMessage = "o campo 'responsavel' é obrigatório")]
    public string? Responsavel { get; set; }

    public string? Observacao { get; set; }
}

public class AjusteRequest
{
    [Required(ErrorMessage = "o campo 'codigo' é obrigatório")]
    public string? Codigo { get; set; }

    /// <summary>Quantidade contada fisicamente no inventário.</summary>
    [Required(ErrorMessage = "o campo 'quantidade_contada' é obrigatório")]
    public int? QuantidadeContada { get; set; }

    [Required(ErrorMessage = "o campo 'responsavel' é obrigatório")]
    public string? Responsavel { get; set; }

    /// <summary>Motivo do ajuste (obrigatório).</summary>
    public string? Observacao { get; set; }
}

/// <summary>Filtros de GET /api/produtos.</summary>
public record FiltroProdutos(
    string? Busca,
    string? Corredor,
    Models.StatusEstoque? Status,
    Models.StatusValidade? Validade,
    string? Ordenar,
    bool IncluirInativos);

/// <summary>Filtros de GET /api/movimentacoes.</summary>
public record FiltroMovimentacoes(
    string? Codigo,
    Models.TipoMovimentacao? Tipo,
    DateOnly? DataInicio,
    DateOnly? DataFim,
    int Limite,
    int Offset);
