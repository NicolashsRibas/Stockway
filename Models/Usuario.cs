using System.Text.RegularExpressions;
using StockWay.Exceptions;

namespace StockWay.Models;

/// <summary>
/// MODEL Usuario: quem pode entrar no sistema (nome, cargo, e-mail e senha).
///
/// Segue a mesma ideia do Produto: cada atributo valida o próprio valor no SETTER,
/// então um Usuario nunca fica em estado inválido. A senha NUNCA é guardada:
/// o construtor e AlterarSenha recebem a senha digitada e guardam só o hash (PBKDF2).
///
/// O Entity Framework lê e grava os campos privados diretamente, por isso as
/// verificações não rodam de novo ao carregar um usuário do banco.
/// </summary>
public class Usuario
{
    public const int NomeMin = 2;
    public const int NomeMax = Movimentacao.ResponsavelMax;   // o nome vira o "responsável" do log
    public const int CargoMin = 2;
    public const int CargoMax = 60;
    public const int EmailMax = 255;
    public const int SenhaMin = 8;
    public const int SenhaMax = 128;
    public const int SenhaHashMax = 255;

    private static readonly Regex EmailValido = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    private string _nome = string.Empty;
    private string _cargo = string.Empty;
    private string _email = string.Empty;

    /// <summary>Usado apenas pelo Entity Framework ao ler do banco.</summary>
    private Usuario() { }

    /// <summary>Cria um usuário válido. Lança RegraNegocioException (400) se algum dado for inválido.</summary>
    public Usuario(string nome, string cargo, string email, string senha)
    {
        Nome = nome;
        Cargo = cargo;
        Email = email;
        AlterarSenha(senha);
        CriadoEm = AtualizadoEm = Relogio.Agora;
    }

    public int Id { get; private set; }

    /// <summary>Nome completo: de 2 a 60 caracteres (mesmo limite do responsável no log).</summary>
    public string Nome
    {
        get => _nome;
        set => _nome = ValidarTexto(value, "nome", NomeMin, NomeMax);
    }

    /// <summary>Cargo/função (ex.: "Almoxarife"): de 2 a 60 caracteres.</summary>
    public string Cargo
    {
        get => _cargo;
        set => _cargo = ValidarTexto(value, "cargo", CargoMin, CargoMax);
    }

    /// <summary>E-mail de acesso. Único, salvo em minúsculas.</summary>
    public string Email
    {
        get => _email;
        private set
        {
            var email = NormalizarEmail(value);
            if (email.Length == 0)
                throw RegraNegocioException.Validacao("o e-mail é obrigatório");
            if (email.Length > EmailMax)
                throw RegraNegocioException.Validacao($"o e-mail deve ter no máximo {EmailMax} caracteres");
            if (!EmailValido.IsMatch(email))
                throw RegraNegocioException.Validacao("o e-mail informado não é válido");
            _email = email;
        }
    }

    /// <summary>Hash da senha no formato "pbkdf2-sha512$iteracoes$salt$hash". Nunca sai na API.</summary>
    public string SenhaHash { get; private set; } = string.Empty;

    /// <summary>Falso quando o acesso do usuário foi bloqueado.</summary>
    public bool Ativo { get; private set; } = true;

    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }
    public DateTime? UltimoAcessoEm { get; private set; }

    // ------------------------------------------------------------------
    // Regras
    // ------------------------------------------------------------------

    /// <summary>Valida a nova senha e guarda apenas o hash dela.</summary>
    public void AlterarSenha(string? senha)
    {
        SenhaHash = HashDeSenha.Gerar(ValidarSenha(senha));
        AtualizadoEm = Relogio.Agora;
    }

    /// <summary>Confere a senha digitada com o hash guardado (comparação em tempo constante).</summary>
    public bool ConferirSenha(string? senha) =>
        !string.IsNullOrEmpty(senha) && HashDeSenha.Verificar(senha, SenhaHash);

    public void RegistrarAcesso() => UltimoAcessoEm = Relogio.Agora;

    public void Desativar()
    {
        Ativo = false;
        AtualizadoEm = Relogio.Agora;
    }

    public void Reativar()
    {
        Ativo = true;
        AtualizadoEm = Relogio.Agora;
    }

    // ------------------------------------------------------------------
    // Auxiliares de verificação
    // ------------------------------------------------------------------

    public static string NormalizarEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>
    /// Regras da senha: de 8 a 128 caracteres, com pelo menos uma letra e um número.
    /// Os espaços são mantidos (fazem parte da senha).
    /// </summary>
    public static string ValidarSenha(string? senha)
    {
        if (string.IsNullOrWhiteSpace(senha))
            throw RegraNegocioException.Validacao("a senha é obrigatória");
        if (senha.Length < SenhaMin || senha.Length > SenhaMax)
            throw RegraNegocioException.Validacao($"a senha deve ter entre {SenhaMin} e {SenhaMax} caracteres");
        if (!senha.Any(char.IsLetter) || !senha.Any(char.IsDigit))
            throw RegraNegocioException.Validacao("a senha deve ter pelo menos uma letra e um número");
        return senha;
    }

    private static string ValidarTexto(string? valor, string campo, int minimo, int maximo)
    {
        var texto = (valor ?? string.Empty).Trim();
        if (texto.Length == 0)
            throw RegraNegocioException.Validacao($"o {campo} é obrigatório");
        if (texto.Any(char.IsControl))
            throw RegraNegocioException.Validacao($"o {campo} contém caracteres inválidos");
        if (texto.Length < minimo || texto.Length > maximo)
            throw RegraNegocioException.Validacao($"o {campo} deve ter entre {minimo} e {maximo} caracteres");
        return texto;
    }
}
