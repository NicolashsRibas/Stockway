using StrockWay.Exceptions;

namespace StrockWay.Models;

/// <summary>
/// MODEL Movimentacao: cada registro do histórico (log) do almoxarifado.
/// Guarda código e nome do produto para o log continuar legível mesmo se o produto mudar.
/// </summary>
public class Movimentacao
{
    public const int ResponsavelMin = 2;
    public const int ResponsavelMax = 60;
    public const int ObservacaoMax = 200;
    public const int ObservacaoMaxInterna = 500;   // descrições geradas pelo sistema (edições)

    public long Id { get; set; }
    public TipoMovimentacao Tipo { get; set; }
    public int ProdutoId { get; set; }
    public string ProdutoCodigo { get; set; } = string.Empty;
    public string ProdutoNome { get; set; } = string.Empty;

    /// <summary>Quantidade movimentada. No AJUSTE pode ser negativa (perda/falta).</summary>
    public int Quantidade { get; set; }

    public int SaldoAnterior { get; set; }
    public int SaldoAtual { get; set; }
    public decimal ValorUnitario { get; set; }

    /// <summary>Quantidade × valor unitário.</summary>
    public decimal ValorTotal { get; set; }

    public string Responsavel { get; set; } = string.Empty;
    public string? Observacao { get; set; }
    public DateTime DataHora { get; set; }

    public static string Descricao(TipoMovimentacao tipo) => tipo switch
    {
        TipoMovimentacao.Cadastro => "Cadastro",
        TipoMovimentacao.Entrada => "Entrada",
        TipoMovimentacao.Saida => "Saída",
        TipoMovimentacao.Ajuste => "Ajuste de inventário",
        TipoMovimentacao.Edicao => "Edição de cadastro",
        TipoMovimentacao.Remocao => "Remoção",
        _ => "Desconhecido"
    };

    // ------------------------------------------------------------------
    // Verificações
    // ------------------------------------------------------------------

    public static void ValidarQuantidade(int quantidade)
    {
        if (quantidade <= 0)
            throw RegraNegocioException.Validacao("a quantidade da movimentação deve ser maior que zero");
        if (quantidade > Produto.QuantidadeLimite)
            throw RegraNegocioException.Validacao($"a quantidade excede o limite de {Produto.QuantidadeLimite} unidades");
    }

    /// <summary>Valida e devolve o nome do responsável sem espaços nas pontas.</summary>
    public static string ValidarResponsavel(string? responsavel)
    {
        var valor = (responsavel ?? string.Empty).Trim();
        if (valor.Length == 0)
            throw RegraNegocioException.Validacao("o responsável pela operação é obrigatório");
        if (valor.Any(char.IsControl))
            throw RegraNegocioException.Validacao("o nome do responsável contém caracteres inválidos");
        if (valor.Length < ResponsavelMin || valor.Length > ResponsavelMax)
            throw RegraNegocioException.Validacao(
                $"o nome do responsável deve ter entre {ResponsavelMin} e {ResponsavelMax} caracteres");
        return valor;
    }

    /// <summary>Valida e devolve a observação sem espaços nas pontas (null se vazia).</summary>
    public static string? ValidarObservacao(string? observacao, bool obrigatoria)
    {
        var valor = (observacao ?? string.Empty).Trim();
        if (valor.Length == 0)
        {
            if (obrigatoria)
                throw RegraNegocioException.Validacao("a observação (motivo) é obrigatória para esta operação");
            return null;
        }
        if (valor.Any(char.IsControl))
            throw RegraNegocioException.Validacao("a observação contém caracteres inválidos (tabulação ou quebra de linha)");
        if (valor.Length > ObservacaoMax)
            throw RegraNegocioException.Validacao($"a observação deve ter no máximo {ObservacaoMax} caracteres");
        return valor;
    }
}
