using Microsoft.EntityFrameworkCore;
using StockWay.Data;
using StockWay.DTOs;
using StockWay.Models;

namespace StockWay.Services;

/// <summary>
/// Monta os dados do relatório diário dos gerentes:
/// entradas e saídas do período, produtos que vencem em até N dias e produtos no estoque mínimo ou abaixo.
/// </summary>
public class RelatorioDiarioService(StockWayContext db, ConfiguracaoRelatorios config)
{
    /// <summary>
    /// Período das movimentações: continua de onde o último relatório automático parou, para que nada
    /// registrado depois do horário de envio fique de fora. Sem relatório anterior (ou se o último é de
    /// mais de um dia atrás), começa à meia-noite de hoje. Termina agora.
    /// </summary>
    public async Task<(DateTime Inicio, DateTime Fim)> PeriodoAtualAsync(CancellationToken cancelamento = default)
    {
        var fim = Relogio.Agora;
        var meiaNoite = fim.Date;

        var ultimoFim = await db.RelatoriosEnviados.AsNoTracking()
            .Where(r => r.Tipo == TipoEnvioRelatorio.Automatico && r.Situacao == SituacaoEnvioRelatorio.Enviado)
            .OrderByDescending(r => r.PeriodoFim)
            .Select(r => (DateTime?)r.PeriodoFim)
            .FirstOrDefaultAsync(cancelamento);

        var inicio = ultimoFim is DateTime anterior && anterior >= meiaNoite.AddDays(-1) && anterior < fim
            ? anterior
            : meiaNoite;
        return (inicio, fim);
    }

    public async Task<RelatorioDiarioResponse> GerarAsync(DateTime inicio, DateTime fim, CancellationToken cancelamento = default)
    {
        var hoje = DateOnly.FromDateTime(fim);
        var produtos = await db.Produtos.AsNoTracking().Where(p => p.Ativo).ToListAsync(cancelamento);
        var movimentos = await db.Movimentacoes.AsNoTracking()
            .Where(m => m.DataHora >= inicio && m.DataHora < fim &&
                        (m.Tipo == TipoMovimentacao.Entrada || m.Tipo == TipoMovimentacao.Saida))
            .ToListAsync(cancelamento);

        TotaisDoPeriodo Totais(TipoMovimentacao tipo)
        {
            var doTipo = movimentos.Where(m => m.Tipo == tipo).ToList();
            return new TotaisDoPeriodo(doTipo.Count, doTipo.Sum(m => m.Quantidade));
        }

        var porProduto = movimentos
            .GroupBy(m => m.ProdutoCodigo)
            .Select(g => new MovimentoDoProduto(
                g.Key,
                g.OrderByDescending(m => m.Id).First().ProdutoNome,
                g.Where(m => m.Tipo == TipoMovimentacao.Entrada).Sum(m => m.Quantidade),
                g.Where(m => m.Tipo == TipoMovimentacao.Saida).Sum(m => m.Quantidade)))
            .OrderByDescending(p => p.UnidadesEntrada + p.UnidadesSaida)
            .ThenBy(p => p.Nome, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        // Vence em até N dias: "vence daqui a 7 dias" já entra no relatório de hoje.
        // Produtos já vencidos que ainda têm saldo também aparecem, no topo da lista.
        // Sem saldo não há o que vencer, então ficam de fora.
        var vencem = produtos
            .Where(p => p.Quantidade > 0 && p.DiasParaVencer(hoje) is int dias && dias <= config.DiasAvisoValidade)
            .OrderBy(p => p.Validade)
            .ThenBy(p => p.Nome, StringComparer.CurrentCultureIgnoreCase)
            .Select(p => ProdutoResponse.De(p, hoje))
            .ToList();

        // Estoque mínimo: zerados ou com saldo menor ou igual ao mínimo, os mais urgentes primeiro.
        var noMinimo = EstoqueService.OrdenarPorUrgencia(produtos.Where(p => p.PrecisaReposicao))
            .Select(p => ProdutoResponse.De(p, hoje))
            .ToList();

        return new RelatorioDiarioResponse(hoje, inicio, fim, Totais(TipoMovimentacao.Entrada), Totais(TipoMovimentacao.Saida),
            porProduto, config.DiasAvisoValidade, vencem, noMinimo);
    }
}
