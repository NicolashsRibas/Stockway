using Microsoft.AspNetCore.Mvc;
using StrockWay.DTOs;

namespace StrockWay.Middleware;

/// <summary>
/// Substitui a resposta padrão de erro de validação do ASP.NET (em inglês) por
/// { "sucesso": false, "erro": "mensagem em português" }.
/// </summary>
public static class ErrosDeValidacao
{
    public static IActionResult CriarResposta(ActionContext contexto)
    {
        var erros = contexto.ModelState
            .Where(e => e.Value is { Errors.Count: > 0 })
            .ToList();

        // O ASP.NET também reporta o parâmetro inteiro ("dados") quando um campo falha; ignoramos esse.
        var relevante = erros.FirstOrDefault(e => e.Key.StartsWith("$") || !EhNomeDeParametro(e.Key));
        if (relevante.Value is null && erros.Count > 0) relevante = erros[0];

        var mensagem = "dados inválidos";
        if (relevante.Value is not null)
        {
            var chave = relevante.Key;
            var erro = relevante.Value.Errors[0];
            var campo = chave.StartsWith("$.") ? chave[2..] : chave;

            if (chave is "" or "$" || EhNomeDeParametro(chave))
                mensagem = "o corpo da requisição está vazio ou não é um JSON válido";
            else if (erro.Exception is not null ||
                     erro.ErrorMessage.Contains("could not be converted", StringComparison.OrdinalIgnoreCase) ||
                     erro.ErrorMessage.Contains("is not valid", StringComparison.OrdinalIgnoreCase))
                mensagem = campo.Contains("validade") || campo.Contains("data")
                    ? $"o campo '{campo}' está em formato inválido; use uma data real no formato AAAA-MM-DD"
                    : $"o campo '{campo}' está em formato inválido";
            else
                mensagem = erro.ErrorMessage;
        }

        return new BadRequestObjectResult(RespostaApi.Falha(mensagem));
    }

    private static bool EhNomeDeParametro(string chave) =>
        chave.Equals("dados", StringComparison.OrdinalIgnoreCase) ||
        chave.Equals("request", StringComparison.OrdinalIgnoreCase);
}
