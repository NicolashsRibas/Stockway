using Microsoft.EntityFrameworkCore;
using StockWay.Data;
using StockWay.Exceptions;
using StockWay.Models;

namespace StockWay.Services;

/// <summary>
/// Envia o relatório diário para todos os usuários ativos com cargo de gerente
/// e grava o resultado na tabela relatorios_enviados.
/// </summary>
public class EnvioDeRelatoriosService(
    StockWayContext db,
    RelatorioDiarioService relatorios,
    IEnvioDeEmail email,
    ConfiguracaoRelatorios config,
    ILogger<EnvioDeRelatoriosService> logger)
{
    public async Task<List<Usuario>> GerentesAsync(CancellationToken cancelamento = default)
    {
        // O cargo é texto livre, então o filtro "contém gerente, sem acento e sem maiúscula" é feito em memória.
        var ativos = await db.Usuarios.AsNoTracking().Where(u => u.Ativo).ToListAsync(cancelamento);
        return ativos.Where(u => config.EhDestinatario(u.Cargo))
                     .OrderBy(u => u.Nome, StringComparer.CurrentCultureIgnoreCase)
                     .ToList();
    }

    public async Task<RelatorioEnviado> EnviarAsync(TipoEnvioRelatorio tipo, string? solicitadoPor,
                                                    CancellationToken cancelamento = default)
    {
        var (inicio, fim) = await relatorios.PeriodoAtualAsync(cancelamento);
        var gerentes = await GerentesAsync(cancelamento);
        var emails = gerentes.Select(g => g.Email).ToList();

        RelatorioEnviado registro;
        if (gerentes.Count == 0)
        {
            registro = new RelatorioEnviado(tipo, inicio, fim, emails, SituacaoEnvioRelatorio.SemDestinatarios,
                $"nenhum usuário ativo com cargo contendo \"{config.CargoDestinatario}\"", solicitadoPor);
            logger.LogWarning("Relatório diário não enviado: nenhum usuário ativo com cargo de gerente.");
        }
        else
        {
            var dados = await relatorios.GerarAsync(inicio, fim, cancelamento);
            var rodape = tipo == TipoEnvioRelatorio.Automatico
                ? $"Enviado automaticamente pelo StockWay às {fim:HH:mm} para quem tem cargo de gerente."
                : $"Enviado pelo StockWay às {fim:HH:mm}, a pedido de {solicitadoPor}, para quem tem cargo de gerente.";
            var (assunto, html, texto) = RelatorioDiarioEmail.Montar(dados, rodape);
            var mensagem = new MensagemDeEmail(gerentes.Select(g => new Destinatario(g.Email, g.Nome)).ToList(), assunto, html, texto);

            try
            {
                var id = await email.EnviarAsync(mensagem, cancelamento);
                registro = new RelatorioEnviado(tipo, inicio, fim, emails, SituacaoEnvioRelatorio.Enviado, id, solicitadoPor);
            }
            catch (FalhaNoEnvioDeEmail ex)
            {
                logger.LogError("Relatório diário não foi enviado: {Motivo}", ex.Message);
                registro = new RelatorioEnviado(tipo, inicio, fim, emails, SituacaoEnvioRelatorio.Falhou, ex.Message, solicitadoPor);
            }
        }

        db.RelatoriosEnviados.Add(registro);
        await db.SaveChangesAsync(cancelamento);
        return registro;
    }

    public async Task<List<RelatorioEnviado>> HistoricoAsync(int limite, CancellationToken cancelamento = default) =>
        await db.RelatoriosEnviados.AsNoTracking()
            .OrderByDescending(r => r.Id)
            .Take(limite)
            .ToListAsync(cancelamento);
}
