using System.Text.Json;
using StrockWay.Exceptions;

namespace StrockWay.Controllers;

/// <summary>Converte textos da query string ("ACIMA_MAXIMO", "baixo") nos enums do sistema.</summary>
public static class EnumTexto
{
    public static T? Ler<T>(string? texto, string parametro) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;

        var normalizado = texto.Trim().Replace("_", string.Empty);
        if (!normalizado.All(char.IsDigit) && Enum.TryParse<T>(normalizado, ignoreCase: true, out var valor))
            return valor;

        var opcoes = string.Join(", ", Enum.GetNames<T>().Select(n => JsonNamingPolicy.SnakeCaseUpper.ConvertName(n)));
        throw RegraNegocioException.Validacao($"parâmetro '{parametro}' inválido; use {opcoes}");
    }
}
