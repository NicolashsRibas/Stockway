using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using StrockWay.Exceptions;

namespace StrockWay.Models;

/// <summary>
/// MODEL Produto: atributos de um item do almoxarifado, campos calculados e verificações.
/// </summary>
public class Produto
{
    public const int CodigoMax = 30;
    public const int NomeMin = 2;
    public const int NomeMax = 100;
    public const int EnderecoMax = 10;
    public const int QuantidadeLimite = 10_000_000;
    public const decimal ValorLimite = 1_000_000_000m;
    public const decimal PesoLimiteKg = 50_000m;
    public const int DiasAlertaValidade = 30;

    private static readonly Regex CodigoValido = new(@"^[A-Za-z0-9._-]+$", RegexOptions.Compiled);
    private static readonly Regex EnderecoValido = new(@"^[A-Za-z0-9-]+$", RegexOptions.Compiled);

    // ------------------------------------------------------------------
    // Atributos (gravados no banco)
    // ------------------------------------------------------------------

    public int Id { get; set; }

    /// <summary>Código único do produto (ex.: "PAR-001"). Sempre em maiúsculas.</summary>
    public string Codigo { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    /// <summary>Saldo atual. Só muda por entrada, saída ou ajuste.</summary>
    public int Quantidade { get; set; }

    /// <summary>Valor de uma unidade (R$).</summary>
    public decimal ValorUnitario { get; set; }

    /// <summary>Peso de uma unidade, em kg.</summary>
    public decimal PesoKg { get; set; }

    /// <summary>Data de validade. Nulo = produto não perecível.</summary>
    public DateOnly? Validade { get; set; }

    /// <summary>Endereço: corredor.</summary>
    public string Corredor { get; set; } = string.Empty;

    /// <summary>Endereço: prateleira.</summary>
    public string Prateleira { get; set; } = string.Empty;

    /// <summary>Indicador de estoque mínimo.</summary>
    public int EstoqueMinimo { get; set; }

    /// <summary>Indicador de estoque máximo.</summary>
    public int EstoqueMaximo { get; set; }

    /// <summary>Falso quando o produto foi removido (exclusão lógica, mantém o histórico).</summary>
    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }

    // ------------------------------------------------------------------
    // Campos calculados (não gravados)
    // ------------------------------------------------------------------

    /// <summary>Valor em estoque = quantidade × valor unitário.</summary>
    [NotMapped]
    public decimal ValorEmEstoque => Math.Round(Quantidade * ValorUnitario, 2);

    /// <summary>Peso total = quantidade × peso unitário.</summary>
    [NotMapped]
    public decimal PesoTotalKg => Quantidade * PesoKg;

    [NotMapped]
    public StatusEstoque Status =>
        Quantidade <= 0 ? StatusEstoque.Zerado :
        Quantidade <= EstoqueMinimo ? StatusEstoque.Baixo :
        Quantidade > EstoqueMaximo ? StatusEstoque.AcimaMaximo :
        StatusEstoque.Normal;

    /// <summary>Verdadeiro se estiver zerado ou com quantidade menor ou igual ao mínimo.</summary>
    [NotMapped]
    public bool PrecisaReposicao => Status is StatusEstoque.Zerado or StatusEstoque.Baixo;

    /// <summary>Quantas unidades faltam para chegar ao máximo (0 se não precisa repor).</summary>
    [NotMapped]
    public int QuantidadeParaRepor => PrecisaReposicao ? Math.Max(0, EstoqueMaximo - Quantidade) : 0;

    [NotMapped]
    public double PercentualOcupacao =>
        EstoqueMaximo > 0 ? Math.Round(Quantidade * 100.0 / EstoqueMaximo, 1) : 0;

    [NotMapped]
    public string EnderecoDescricao => $"Corredor {Corredor} - Prateleira {Prateleira}";

    /// <summary>Dias até vencer (negativo = vencido). Nulo se não tiver validade.</summary>
    public int? DiasParaVencer(DateOnly hoje) =>
        Validade is null ? null : Validade.Value.DayNumber - hoje.DayNumber;

    public StatusValidade ObterStatusValidade(DateOnly hoje)
    {
        var dias = DiasParaVencer(hoje);
        if (dias is null) return StatusValidade.NaoPerecivel;
        if (dias < 0) return StatusValidade.Vencido;
        if (dias <= DiasAlertaValidade) return StatusValidade.VenceEmBreve;
        return StatusValidade.Ok;
    }

    // ------------------------------------------------------------------
    // Normalização e verificações
    // ------------------------------------------------------------------

    public static string NormalizarCodigo(string? codigo) => (codigo ?? string.Empty).Trim().ToUpperInvariant();

    public void Normalizar()
    {
        Codigo = NormalizarCodigo(Codigo);
        Nome = (Nome ?? string.Empty).Trim();
        Corredor = (Corredor ?? string.Empty).Trim().ToUpperInvariant();
        Prateleira = (Prateleira ?? string.Empty).Trim().ToUpperInvariant();
        ValorUnitario = Math.Round(ValorUnitario, 2);
    }

    /// <summary>Executa todas as verificações. Lança RegraNegocioException (400) se algo estiver inválido.</summary>
    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(Codigo))
            throw RegraNegocioException.Validacao("o código do produto é obrigatório");
        if (Codigo.Length > CodigoMax)
            throw RegraNegocioException.Validacao($"o código do produto deve ter no máximo {CodigoMax} caracteres");
        if (!CodigoValido.IsMatch(Codigo))
            throw RegraNegocioException.Validacao("o código do produto deve conter apenas letras, números, '-', '_' ou '.'");

        if (string.IsNullOrWhiteSpace(Nome))
            throw RegraNegocioException.Validacao("o nome do produto é obrigatório");
        if (Nome.Any(char.IsControl))
            throw RegraNegocioException.Validacao("o nome do produto contém caracteres inválidos");
        if (Nome.Length < NomeMin || Nome.Length > NomeMax)
            throw RegraNegocioException.Validacao($"o nome do produto deve ter entre {NomeMin} e {NomeMax} caracteres");

        ValidarCampoEndereco("corredor", Corredor);
        ValidarCampoEndereco("prateleira", Prateleira);
        ValidarValorUnitario(ValorUnitario);

        if (PesoKg <= 0)
            throw RegraNegocioException.Validacao("o peso (peso_kg) deve ser maior que zero");
        if (PesoKg > PesoLimiteKg)
            throw RegraNegocioException.Validacao($"o peso de uma unidade excede o limite de {PesoLimiteKg:N0} kg");

        if (Quantidade < 0)
            throw RegraNegocioException.Validacao("a quantidade não pode ser negativa");
        if (Quantidade > QuantidadeLimite)
            throw RegraNegocioException.Validacao($"a quantidade excede o limite de {QuantidadeLimite} unidades");

        if (EstoqueMinimo < 0)
            throw RegraNegocioException.Validacao("o estoque mínimo não pode ser negativo");
        if (EstoqueMaximo <= 0)
            throw RegraNegocioException.Validacao("o estoque máximo deve ser maior que zero");
        if (EstoqueMaximo > QuantidadeLimite)
            throw RegraNegocioException.Validacao($"o estoque máximo excede o limite de {QuantidadeLimite} unidades");
        if (EstoqueMinimo >= EstoqueMaximo)
            throw RegraNegocioException.Validacao(
                $"o estoque mínimo ({EstoqueMinimo}) deve ser menor que o estoque máximo ({EstoqueMaximo})");
    }

    /// <summary>Uma validade nova (cadastro ou alteração) não pode estar vencida.</summary>
    public void ValidarValidadeNaoVencida(DateOnly hoje)
    {
        if (Validade is DateOnly data && data < hoje)
            throw RegraNegocioException.Validacao($"a validade informada ({data:yyyy-MM-dd}) já está vencida");
    }

    public static void ValidarValorUnitario(decimal valor)
    {
        if (valor < 0)
            throw RegraNegocioException.Validacao("o valor unitário não pode ser negativo");
        if (valor > ValorLimite)
            throw RegraNegocioException.Validacao("o valor unitário excede o limite permitido");
    }

    private static void ValidarCampoEndereco(string campo, string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw RegraNegocioException.Validacao($"o campo '{campo}' do endereço é obrigatório");
        if (valor.Length > EnderecoMax)
            throw RegraNegocioException.Validacao($"o campo '{campo}' deve ter no máximo {EnderecoMax} caracteres");
        if (!EnderecoValido.IsMatch(valor))
            throw RegraNegocioException.Validacao($"o campo '{campo}' deve conter apenas letras, números ou '-'");
    }
}
