using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrockWay.Data;
using StrockWay.DTOs;
using StrockWay.Services;

namespace StrockWay.Controllers;

/// <summary>Indicadores da tela inicial e verificação de funcionamento.</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class DashboardController(DashboardService dashboard, StrockWayContext db) : ControllerBase
{
    /// <summary>Tudo o que a dashboard do site precisa: resumo, gráficos, alertas e últimas movimentações.</summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<RespostaApi<DashboardResponse>>> Exibir() =>
        Ok(RespostaApi.Ok(await dashboard.GerarAsync()));

    /// <summary>Verifica se a API está no ar.</summary>
    [HttpGet("health")]
    public async Task<ActionResult<RespostaApi<HealthResponse>>> Health() =>
        Ok(RespostaApi.Ok(new HealthResponse(
            "ok",
            await db.Produtos.CountAsync(),
            await db.Movimentacoes.CountAsync(),
            Relogio.Agora)));
}
