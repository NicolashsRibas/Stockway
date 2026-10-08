using StrockWay.Exceptions;

namespace StrockWay.Models;

/// <summary>
/// MODEL Movimentacao: cada registro do histórico (log) do almoxarifado.
///
/// É um registro de auditoria, por isso é imutável: só é criado pelo construtor e
/// todos os setters são privados. Guarda código e nome do produto para o log
/// continuar legível mesmo se o produto mudar depois.
/// </summary>
public class Movimentacao
{
    public const int ResponsavelMin = 2;
    public const int ResponsavelMax = 60;
    public const int ObservacaoMax = 200;          // texto digitado pelo usuário
    public const int ObservacaoMaxInterna = 500;   // descrições geradas pelo sistema (edições)

    private string _responsavel = string.Empty;
    private string? _observacao;

    /// <summary>Usado apenas pelo Entity Framework ao ler do banco.</summary>
    private Movimentacao() { }

    /// <summary>Cria o registro a partir do produto já atualizado (SaldoAtual = produto.Quantidade).</summary>
    public Movimentacao(TipoMovimentacao tipo, Produto produto, int quantidade, int saldoAnterior,
                        decimal valorUnitario, string responsavel, string? observacao)
    {
        Tipo = tipo;
        ProdutoId = produto.Id;
        ProdutoCodigo = produto.Codigo;
        ProdutoNome = produto.Nome;
        Quantidade = quantidade;
        SaldoAnterior = saldoAnterior;
        SaldoAtual = produto.Quantidade;
        ValorUnitario = Math.Round(valorUnitario, 2);
        ValorTotal = Math.Round(quantidade * valorUnitario, 2);
        Responsavel = responsavel;
        Observacao = observacao;
        DataHora = Relogio.Agora;
    }

    public long Id { get; private set; }
    public TipoMovimentacao Tipo { get; private set; }
    public int ProdutoId { get; private set; }
    public string ProdutoCodigo { get; private set; } = string.Empty;
    public string ProdutoNome { get; private set; } = string.Empty;

    /// <summary>Quantidade movimentada. No AJUSTE pode ser negativa (perda/falta).</summary>
    public int Quantidade { get; private set; }

    public int SaldoAnterior { get; private set; }
    public int SaldoAtual { get; private set; }
    public decimal ValorUnitario { get; private set; }

    /// <summary>Quantidade × valor unitário.</summary>
    public decimal ValorTotal { get; private set; }

    /// <summary>Quem fez a operação: de 2 a 60 caracteres.</summary>
    public string Responsavel
    {
        get => _responsavel;
        private set => _responsavel = ValidarResponsavel(value);
    }

    /// <summary>Observação/motivo. Textos do sistema maiores que 500 caracteres são cortados.</summary>
    public string? Observacao
    {
        get => _observacao;
        private set
        {
            var texto = value?.Trim();
            if (string.IsNullOrEmpty(texto))
            {
                _observacao = null;
                return;
            }
            if (texto.Any(char.IsControl))
                throw RegraNegocioException.Validacao("a observação contém caracteres inválidos (tabulação ou quebra de linha)");
            _observacao = texto.Length > ObservacaoMaxInterna ? texto[..ObservacaoMaxInterna] : texto;
        }
    }

    public DateTime DataHora { get; private set; }

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
    // Verificações (também usadas pelo Service antes de alterar o produto)
    // ------------------------------------------------------------------

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

    /// <summary>Valida a observação digitada pelo usuário (até 200 caracteres). Devolve null se vazia.</summary>
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
