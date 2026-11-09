using Microsoft.EntityFrameworkCore;
using StockWay.Data;
using StockWay.Models;

namespace StockWay.Services;

/// <summary>
/// Roda junto com a API e envia o relatório diário aos gerentes no fim do dia.
///
/// A cada minuto confere: já passou do horário (Relatorios:Horario) e o relatório automático de hoje
/// ainda não saiu? Então envia. Assim o envio acontece mesmo se a API estava desligada no horário
/// (sai assim que ela ligar, no mesmo dia) e não se repete se a API reiniciar depois do envio.
/// Se o Brevo falhar, tenta de novo a cada 30 minutos, até 5 vezes no dia.
/// </summary>
public class AgendadorDeRelatorios(
    IServiceScopeFactory escopos,
    ConfiguracaoRelatorios config,
    ConfiguracaoBrevo brevo,
    ILogger<AgendadorDeRelatorios> logger) : BackgroundService
{
    private const int MaximoDeTentativasPorDia = 5;
    private static readonly TimeSpan EsperaAposFalha = TimeSpan.FromMinutes(30);

    private DateTime? _proximaTentativa;

    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        if (!config.EnvioAutomatico)
        {
            logger.LogInformation("Relatório diário: envio automático desligado (Relatorios:EnvioAutomatico = false).");
            return;
        }
        if (!brevo.Configurado)
        {
            logger.LogWarning("Relatório diário: envio automático parado. Defina Brevo__ChaveApi e Brevo__RemetenteEmail para ligar.");
            return;
        }

        logger.LogInformation("Relatório diário: envio automático todos os dias às {Horario} para quem tem cargo \"{Cargo}\".",
            config.HorarioDeEnvio.ToString("HH:mm"), config.CargoDestinatario);

        using var relogio = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                await VerificarAsync(parada);
            }
            catch (Exception ex) when (!parada.IsCancellationRequested)
            {
                // Um erro (ex.: banco fora do ar) não pode derrubar o agendador: tenta de novo no próximo minuto.
                logger.LogError(ex, "Relatório diário: erro ao verificar o envio.");
            }
        }
        while (await relogio.WaitForNextTickAsync(parada));
    }

    private async Task VerificarAsync(CancellationToken parada)
    {
        var agora = Relogio.Agora;
        if (TimeOnly.FromDateTime(agora) < config.HorarioDeEnvio) return;
        if (_proximaTentativa is DateTime proxima && agora < proxima) return;

        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<StockWayContext>();
        var hoje = DateOnly.FromDateTime(agora);

        var deHoje = await db.RelatoriosEnviados.AsNoTracking()
            .Where(r => r.Tipo == TipoEnvioRelatorio.Automatico && r.DataReferencia == hoje)
            .Select(r => r.Situacao)
            .ToListAsync(parada);

        if (deHoje.Any(s => s != SituacaoEnvioRelatorio.Falhou)) return;          // já saiu (ou não havia gerentes)
        if (deHoje.Count >= MaximoDeTentativasPorDia) return;                    // desiste até amanhã

        var envio = escopo.ServiceProvider.GetRequiredService<EnvioDeRelatoriosService>();
        var resultado = await envio.EnviarAsync(TipoEnvioRelatorio.Automatico, null, parada);

        _proximaTentativa = resultado.Situacao == SituacaoEnvioRelatorio.Falhou ? agora.Add(EsperaAposFalha) : null;
        logger.LogInformation("Relatório diário de {Data}: {Situacao} para {Quantidade} destinatário(s).",
            hoje.ToString("dd/MM/yyyy"), resultado.Situacao, resultado.QuantidadeDestinatarios);
    }
}
