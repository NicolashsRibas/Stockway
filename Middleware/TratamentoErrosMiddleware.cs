using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MySqlConnector;
using StockWay.DTOs;
using StockWay.Exceptions;

namespace StockWay.Middleware;

/// <summary>
/// Converte exceções em respostas JSON padronizadas:
/// RegraNegocioException -> código indicado (400, 401, 403, 404, 409, 422);
/// conflitos de gravação no banco -> 409; demais -> 500.
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
        catch (DbUpdateConcurrencyException)
        {
            // Outra operação alterou o mesmo produto ao mesmo tempo (ver StockWayContext).
            await EscreverErroAsync(contexto, StatusCodes.Status409Conflict,
                "o registro foi alterado por outra operação ao mesmo tempo; recarregue os dados e tente novamente");
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
        {
            // Proteção extra: o índice único do banco recusou um código de produto ou e-mail repetido.
            await EscreverErroAsync(contexto, StatusCodes.Status409Conflict,
                "já existe um registro com esse valor (código do produto ou e-mail repetido)");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado em {Metodo} {Caminho}", contexto.Request.Method, contexto.Request.Path);
            await EscreverErroAsync(contexto, StatusCodes.Status500InternalServerError, "erro interno no servidor");
        }
    }

    /// <summary>Escreve { "sucesso": false, "erro": "..." }. Também usado pelo login (401/403) e pelo limite de tentativas (429).</summary>
    public static async Task EscreverErroAsync(HttpContext contexto, int status, string mensagem)
    {
        if (contexto.Response.HasStarted) return;

        var opcoesJson = contexto.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
        contexto.Response.StatusCode = status;
        await contexto.Response.WriteAsJsonAsync(RespostaApi.Falha(mensagem), opcoesJson);
    }
}
