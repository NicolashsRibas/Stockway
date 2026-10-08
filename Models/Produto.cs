using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using StrockWay.Exceptions;

namespace StrockWay.Models;

/// <summary>
/// MODEL Produto: atributos de um item do almoxarifado, campos calculados e verificações.
///
/// Cada atributo valida o próprio valor no SETTER, então um Produto nunca fica em
/// estado inválido. Regras que envolvem mais de um atributo ficam em métodos:
/// DefinirLimites, RegistrarEntrada, RegistrarSaida, AjustarPara e Desativar.
///
/// O Entity Framework lê e grava os campos privados (_nome, _pesoKg...) diretamente,
/// por isso as verificações não rodam de novo ao carregar um produto do banco.
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

    // Campos privados que guardam os valores já verificados
    private string _codigo = string.Empty;
    private string _nome = string.Empty;
    private int _quantidade;
    private decimal _valorUnitario;
    private decimal _pesoKg;
    private DateOnly? _validade;
    private string _corredor = string.Empty;
    private string _prateleira = string.Empty;

    // ------------------------------------------------------------------
    // Construtores
    // ------------------------------------------------------------------

    /// <summary>Usado apenas pelo Entity Framework ao ler do banco.</summary>
    private Produto() { }

    /// <summary>Cria um produto válido. Lança RegraNegocioException (400) se algum dado for inválido.</summary>
    public Produto(string codigo, string nome, decimal valorUnitario, decimal pesoKg,
                   string corredor, string prateleira, int estoqueMinimo, int estoqueMaximo,
                   int quantidadeInicial = 0, DateOnly? validade = null)
    {
        Codigo = codigo;
        Nome = nome;
        ValorUnitario = valorUnitario;
        PesoKg = pesoKg;
        Corredor = corredor;
        Prateleira = prateleira;
        DefinirLimites(estoqueMinimo, estoqueMaximo);
        Validade = validade;
        Quantidade = quantidadeInicial;

        if (quantidadeInicial > estoqueMaximo)
            throw RegraNegocioException.Validacao(
                $"a quantidade inicial ({quantidadeInicial}) excede o estoque máximo ({estoqueMaximo})");
    }

    // ------------------------------------------------------------------
    // Atributos com verificação no setter
    // ------------------------------------------------------------------

    public int Id { get; private set; }

    /// <summary>Código único (ex.: "PAR-001"). Salvo em maiúsculas e não pode ser alterado depois do cadastro.</summary>
    public string Codigo
    {
        get => _codigo;
        private set
        {
            var codigo = NormalizarCodigo(value);
            if (codigo.Length == 0)
                throw RegraNegocioException.Validacao("o código do produto é obrigatório");
            if (codigo.Length > CodigoMax)
                throw RegraNegocioException.Validacao($"o código do produto deve ter no máximo {CodigoMax} caracteres");
            if (!CodigoValido.IsMatch(codigo))
                throw RegraNegocioException.Validacao("o código do produto deve conter apenas letras, números, '-', '_' ou '.'");
            _codigo = codigo;
        }
    }

    /// <summary>Nome do produto: de 2 a 100 caracteres.</summary>
    public string Nome
    {
        get => _nome;
        set
        {
            var nome = (value ?? string.Empty).Trim();
            if (nome.Length == 0)
                throw RegraNegocioException.Validacao("o nome do produto é obrigatório");
            if (nome.Any(char.IsControl))
                throw RegraNegocioException.Validacao("o nome do produto contém caracteres inválidos");
            if (nome.Length < NomeMin || nome.Length > NomeMax)
                throw RegraNegocioException.Validacao($"o nome do produto deve ter entre {NomeMin} e {NomeMax} caracteres");
            _nome = nome;
        }
    }

    /// <summary>Saldo atual. Só muda pelos métodos de movimentação (entrada, saída, ajuste).</summary>
    public int Quantidade
    {
        get => _quantidade;
        private set
        {
            if (value < 0)
                throw RegraNegocioException.Validacao("a quantidade não pode ser negativa");
            if (value > QuantidadeLimite)
                throw RegraNegocioException.Validacao($"a quantidade excede o limite de {QuantidadeLimite} unidades");
            _quantidade = value;
        }
    }

    /// <summary>Valor de uma unidade (R$), arredondado para 2 casas.</summary>
    public decimal ValorUnitario
    {
        get => _valorUnitario;
        set => _valorUnitario = Math.Round(VerificarValor(value), 2);
    }

    /// <summary>Peso de UMA unidade, em kg: maior que zero e até 50.000 kg.</summary>
    public decimal PesoKg
    {
        get => _pesoKg;
        set
        {
            if (value <= 0)
                throw RegraNegocioException.Validacao("o peso (peso_kg) deve ser maior que zero");
            if (value > PesoLimiteKg)
                throw RegraNegocioException.Validacao($"o peso de uma unidade excede o limite de {PesoLimiteKg:N0} kg");
            _pesoKg = value;
        }
    }

    /// <summary>Data de validade. Nulo = não perecível. Uma data nova não pode estar vencida.</summary>
    public DateOnly? Validade
    {
        get => _validade;
        set
        {
            if (value == _validade) return;
            if (value is DateOnly data && data < Relogio.Hoje)
                throw RegraNegocioException.Validacao($"a validade informada ({data:yyyy-MM-dd}) já está vencida");
            _validade = value;
        }
    }

    /// <summary>Endereço: corredor (até 10 caracteres: letras, números ou '-').</summary>
    public string Corredor
    {
        get => _corredor;
        set => _corredor = VerificarEndereco("corredor", value);
    }

    /// <summary>Endereço: prateleira (até 10 caracteres: letras, números ou '-').</summary>
    public string Prateleira
    {
        get => _prateleira;
        set => _prateleira = VerificarEndereco("prateleira", value);
    }

    /// <summary>Indicador de estoque mínimo. Altere com DefinirLimites.</summary>
    public int EstoqueMinimo { get; private set; }

    /// <summary>Indicador de estoque máximo. Altere com DefinirLimites.</summary>
    public int EstoqueMaximo { get; private set; }

    /// <summary>Falso quando o produto foi removido (exclusão lógica, mantém o histórico).</summary>
    public bool Ativo { get; private set; } = true;

    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }

    // ------------------------------------------------------------------
    // Regras que envolvem mais de um atributo
    // ------------------------------------------------------------------

    /// <summary>
    /// Define mínimo e máximo juntos, porque um depende do outro (mínimo &lt; máximo).
    /// Com setters separados, a ordem de atribuição poderia gerar um erro falso.
    /// </summary>
    public void DefinirLimites(int minimo, int maximo)
    {
        if (minimo < 0)
            throw RegraNegocioException.Validacao("o estoque mínimo não pode ser negativo");
        if (maximo <= 0)
            throw RegraNegocioException.Validacao("o estoque máximo deve ser maior que zero");
        if (maximo > QuantidadeLimite)
            throw RegraNegocioException.Validacao($"o estoque máximo excede o limite de {QuantidadeLimite} unidades");
        if (minimo >= maximo)
            throw RegraNegocioException.Validacao(
                $"o estoque mínimo ({minimo}) deve ser menor que o estoque máximo ({maximo})");

        EstoqueMinimo = minimo;
        EstoqueMaximo = maximo;
    }

    /// <summary>
    /// Soma ao saldo. Recusa (422) se ultrapassar o estoque máximo.
    /// Se o custo da compra for informado, o valor unitário passa a ser o custo médio ponderado.
    /// </summary>
    public void RegistrarEntrada(int quantidade, decimal? custoUnitario = null)
    {
        VerificarQuantidadeMovimentada(quantidade);
        if (custoUnitario is decimal custo) VerificarValor(custo);

        if ((long)Quantidade + quantidade > EstoqueMaximo)
        {
            var disponivel = Math.Max(0, EstoqueMaximo - Quantidade);
            throw RegraNegocioException.Regra(
                $"a entrada de {quantidade} unidade(s) ultrapassa o estoque máximo de '{Codigo}' " +
                $"(saldo atual {Quantidade}, máximo {EstoqueMaximo}, capacidade disponível {disponivel})");
        }

        if (custoUnitario is decimal novoCusto)
            ValorUnitario = (Quantidade * ValorUnitario + quantidade * novoCusto) / (Quantidade + quantidade);

        Quantidade += quantidade;
    }

    /// <summary>Subtrai do saldo. Recusa (422) se a quantidade for maior que o disponível.</summary>
    public void RegistrarSaida(int quantidade)
    {
        VerificarQuantidadeMovimentada(quantidade);
        if (quantidade > Quantidade)
            throw RegraNegocioException.Regra(
                $"estoque insuficiente para '{Codigo}': solicitado {quantidade}, disponível {Quantidade}");

        Quantidade -= quantidade;
    }

    /// <summary>Ajuste de inventário: o saldo passa a ser a quantidade contada fisicamente.</summary>
    public void AjustarPara(int quantidadeContada)
    {
        if (quantidadeContada < 0)
            throw RegraNegocioException.Validacao("a quantidade contada não pode ser negativa");
        if (quantidadeContada == Quantidade)
            throw RegraNegocioException.Validacao(
                $"a quantidade contada é igual ao saldo atual ({Quantidade}); nenhum ajuste é necessário");

        Quantidade = quantidadeContada;
    }

    /// <summary>Remove o produto do cadastro (exclusão lógica). Só é permitido com saldo zero.</summary>
    public void Desativar()
    {
        if (Quantidade > 0)
            throw RegraNegocioException.Conflito(
                $"não é possível remover '{Codigo}': ainda há {Quantidade} unidade(s) em estoque. " +
                "Registre a saída ou um ajuste antes de remover");
        Ativo = false;
    }

    // ------------------------------------------------------------------
    // Campos calculados (não gravados no banco)
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
    // Auxiliares de verificação
    // ------------------------------------------------------------------

    public static string NormalizarCodigo(string? codigo) => (codigo ?? string.Empty).Trim().ToUpperInvariant();

    private static decimal VerificarValor(decimal valor)
    {
        if (valor < 0)
            throw RegraNegocioException.Validacao("o valor unitário não pode ser negativo");
        if (valor > ValorLimite)
            throw RegraNegocioException.Validacao("o valor unitário excede o limite permitido");
        return valor;
    }

    private static string VerificarEndereco(string campo, string? valor)
    {
        var texto = (valor ?? string.Empty).Trim().ToUpperInvariant();
        if (texto.Length == 0)
            throw RegraNegocioException.Validacao($"o campo '{campo}' do endereço é obrigatório");
        if (texto.Length > EnderecoMax)
            throw RegraNegocioException.Validacao($"o campo '{campo}' deve ter no máximo {EnderecoMax} caracteres");
        if (!EnderecoValido.IsMatch(texto))
            throw RegraNegocioException.Validacao($"o campo '{campo}' deve conter apenas letras, números ou '-'");
        return texto;
    }

    private static void VerificarQuantidadeMovimentada(int quantidade)
    {
        if (quantidade <= 0)
            throw RegraNegocioException.Validacao("a quantidade da movimentação deve ser maior que zero");
        if (quantidade > QuantidadeLimite)
            throw RegraNegocioException.Validacao($"a quantidade excede o limite de {QuantidadeLimite} unidades");
    }
}
