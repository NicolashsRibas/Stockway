using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StrockWay.DTOs;
using StrockWay.Exceptions;

namespace StrockWay.Middleware;

/// <summary>
/// Converte exceções em respostas JSON padronizadas:
/// RegraNegocioException -> código indicado (400, 404, 409, 422); demais -> 500.
/// </summary>
public class TratamentoErrosMiddleware(RequestDelegate proximo, ILogger<TratamentoErrosMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await proximo(contexto);
        }
        catch (RegraNegocioException ex)
        {
            await EscreverErroAsync(contexto, ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado em {Metodo} {Caminho}", contexto.Request.Method, contexto.Request.Path);
            await EscreverErroAsync(contexto, StatusCodes.Status500InternalServerError, "erro interno no servidor");
        }
    }

    private static async Task EscreverErroAsync(HttpContext contexto, int status, string mensagem)
    {
        if (contexto.Response.HasStarted) return;

        var opcoesJson = contexto.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
        contexto.Response.StatusCode = status;
        await contexto.Response.WriteAsJsonAsync(RespostaApi.Falha(mensagem), opcoesJson);
    }
}
