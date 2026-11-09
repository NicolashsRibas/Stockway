using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockWay.Data;
using StockWay.DTOs;
using StockWay.Services;

namespace StockWay.Controllers;

/// <summary>Indicadores da tela inicial e verificação de funcionamento.</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class DashboardController(DashboardService dashboard, StockWayContext db) : ControllerBase
{
    /// <summary>Tudo o que a dashboard do site precisa: resumo, gráficos, alertas e últimas movimentações.</summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<RespostaApi<DashboardResponse>>> Exibir() =>
        Ok(RespostaApi.Ok(await dashboard.GerarAsync()));

    /// <summary>Verifica se a API está no ar. Não exige login (a tela de entrada usa esta rota).</summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<ActionResult<RespostaApi<HealthResponse>>> Health() =>
        Ok(RespostaApi.Ok(new HealthResponse(
            "ok",
            await db.Produtos.CountAsync(),
            await db.Movimentacoes.CountAsync(),
            Relogio.Agora)));
}
