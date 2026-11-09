using System.Globalization;
using System.Security.Claims;

namespace StockWay.Common;

/// <summary>Nomes das informações (claims) guardadas no token e atalhos para lê-las nos Controllers.</summary>
public static class UsuarioLogado
{
    public const string ClaimId = "sub";
    public const string ClaimNome = "name";
    public const string ClaimEmail = "email";
    public const string ClaimCargo = "cargo";

    public static bool EstaLogado(this ClaimsPrincipal usuario) => usuario.Identity?.IsAuthenticated == true;

    public static int? IdDoUsuario(this ClaimsPrincipal usuario) =>
        usuario.EstaLogado() &&
        int.TryParse(usuario.FindFirstValue(ClaimId), NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;

    /// <summary>
    /// Quem está fazendo a operação. Com login, o nome vem do token, e o que foi enviado no
    /// corpo é ignorado (ninguém consegue registrar algo em nome de outra pessoa).
    /// Sem login, vale o "responsavel" enviado, como antes.
    /// </summary>
    public static string? ResponsavelOu(this ClaimsPrincipal usuario, string? informado) =>
        usuario.EstaLogado() ? usuario.FindFirstValue(ClaimNome) : informado;
}
