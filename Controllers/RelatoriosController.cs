using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockWay.DTOs;
using StockWay.Exceptions;
using StockWay.Models;
using StockWay.Services;

namespace StockWay.Controllers;

/// <summary>
/// Relatório diário dos gerentes. O envio normal é automático, no fim do dia (AgendadorDeRelatorios);
/// estas rotas servem para conferir o relatório e mandar na hora. Só para usuários logados com cargo de gerente.
/// </summary>
[ApiController]
[Route("api/relatorios")]
[Produces("application/json")]
[Authorize(Policy = PoliticaGerente)]
public class RelatoriosController(RelatorioDiarioService relatorios, EnvioDeRelatoriosService envio) : ControllerBase
{
    public const string PoliticaGerente = "Gerente";

    /// <summary>Prévia do relatório que será enviado no fim do dia (não envia nada).</summary>
    [HttpGet("diario")]
    public async Task<ActionResult<RespostaApi<RelatorioDiarioResponse>>> Previa(CancellationToken cancelamento)
    {
        var (inicio, fim) = await relatorios.PeriodoAtualAsync(cancelamento);
        return Ok(RespostaApi.Ok(await relatorios.GerarAsync(inicio, fim, cancelamento)));
    }

    /// <summary>Envia o relatório agora para todos os gerentes (não muda o envio automático do fim do dia).</summary>
    [HttpPost("diario/enviar")]
    public async Task<ActionResult<RespostaApi<EnvioRelatorioResponse>>> EnviarAgora(CancellationToken cancelamento)
    {
        var registro = await envio.EnviarAsync(TipoEnvioRelatorio.Manual, User.Identity?.Name, cancelamento);
        return registro.Situacao switch
        {
            SituacaoEnvioRelatorio.Enviado => StatusCode(StatusCodes.Status201Created, RespostaApi.Ok(EnvioRelatorioResponse.De(registro))),
            SituacaoEnvioRelatorio.SemDestinatarios => throw RegraNegocioException.Regra(registro.Mensagem ?? "nenhum gerente cadastrado"),
            _ => throw RegraNegocioException.ServicoExterno($"o relatório não foi enviado: {registro.Mensagem}")
        };
    }

    /// <summary>Histórico dos envios (automáticos e manuais), mais recentes primeiro.</summary>
    /// <param name="limite">1 a 200 (padrão 30).</param>
    /// <param name="cancelamento">Cancelado se o cliente desistir da requisição.</param>
    [HttpGet("envios")]
    public async Task<ActionResult<RespostaApi<List<EnvioRelatorioResponse>>>> Historico([FromQuery] int limite = 30,
        CancellationToken cancelamento = default)
    {
        if (limite is < 1 or > 200)
            throw RegraNegocioException.Validacao("parâmetro 'limite' deve ser um inteiro entre 1 e 200");
        var envios = await envio.HistoricoAsync(limite, cancelamento);
        return Ok(RespostaApi.Ok(envios.Select(EnvioRelatorioResponse.De).ToList()));
    }
}
