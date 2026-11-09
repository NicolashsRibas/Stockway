namespace StockWay.Exceptions;

/// <summary>
/// Erro de regra de negócio ou de validação. O middleware converte em
/// { "sucesso": false, "erro": "mensagem" } com o código HTTP indicado.
/// </summary>
public class RegraNegocioException(int statusCode, string mensagem) : Exception(mensagem)
{
    public int StatusCode { get; } = statusCode;

    /// <summary>400 - dados inválidos.</summary>
    public static RegraNegocioException Validacao(string mensagem) => new(400, mensagem);

    /// <summary>401 - não está logado, e-mail/senha inválidos ou token vencido.</summary>
    public static RegraNegocioException NaoAutenticado(string mensagem) => new(401, mensagem);

    /// <summary>403 - logado, mas sem permissão (ex.: usuário desativado).</summary>
    public static RegraNegocioException Proibido(string mensagem) => new(403, mensagem);

    /// <summary>404 - registro não encontrado.</summary>
    public static RegraNegocioException NaoEncontrado(string mensagem) => new(404, mensagem);

    /// <summary>409 - conflito (código duplicado, remoção de produto com saldo).</summary>
    public static RegraNegocioException Conflito(string mensagem) => new(409, mensagem);

    /// <summary>422 - operação inválida para o estado atual (saldo insuficiente, acima do máximo).</summary>
    public static RegraNegocioException Regra(string mensagem) => new(422, mensagem);

    /// <summary>502 - um serviço externo (ex.: o Brevo, de e-mail) recusou ou não respondeu.</summary>
    public static RegraNegocioException ServicoExterno(string mensagem) => new(502, mensagem);
}
