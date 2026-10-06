using Microsoft.EntityFrameworkCore;
using StrockWay.Data;
using StrockWay.DTOs;
using StrockWay.Models;

namespace StrockWay.Services;

/// <summary>Monta os indicadores da dashboard e as listas de alertas.</summary>
public class DashboardService(StrockWayContext db)
{
    private const int MaxAlertas = 10;
    private const int MaxRanking = 5;
    private const int MaxUltimas = 10;
    private const int DiasGrafico = 7;

    public async Task<DashboardResponse> GerarAsync()
    {
        var hoje = EstoqueService.Hoje;
        var produtos = await db.Produtos.AsNoTracking().Where(p => p.Ativo).ToListAsync();

        // ---- Resumo ----
        var resumo = new ResumoDashboard(
            TotalProdutos: produtos.Count,
            TotalUnidades: produtos.Sum(p => p.Quantidade),
            ValorTotalEstoque: produtos.Sum(p => p.ValorEmEstoque),
            PesoTotalKg: produtos.Sum(p => p.PesoTotalKg),
            ProdutosBaixoEstoque: produtos.Count(p => p.PrecisaReposicao),
            ProdutosVencidos: produtos.Count(p => p.ObterStatusValidade(hoje) == StatusValidade.Vencido),
            ProdutosVencendoEmBreve: produtos.Count(p => p.ObterStatusValidade(hoje) == StatusValidade.VenceEmBreve),
            TotalMovimentacoes: await db.Movimentacoes.CountAsync());

        var porStatus = new EstoquePorStatus(
            produtos.Count(p => p.Status == StatusEstoque.Zerado),
            produtos.Count(p => p.Status == StatusEstoque.Baixo),
            produtos.Count(p => p.Status == StatusEstoque.Normal),
            produtos.Count(p => p.Status == StatusEstoque.AcimaMaximo));

        // ---- Movimentações de hoje e dos últimos 7 dias ----
        var inicioPeriodo = hoje.AddDays(-(DiasGrafico - 1)).ToDateTime(TimeOnly.MinValue);
        var movimentosPeriodo = await db.Movimentacoes.AsNoTracking()
            .Where(m => m.DataHora >= inicioPeriodo &&
                        (m.Tipo == TipoMovimentacao.Entrada || m.Tipo == TipoMovimentacao.Saida ||
                         m.Tipo == TipoMovimentacao.Ajuste))
            .ToListAsync();

        var deHoje = movimentosPeriodo.Where(m => DateOnly.FromDateTime(m.DataHora) == hoje).ToList();
        var movimentacoesHoje = new MovimentacoesHoje(
            Totais(deHoje, TipoMovimentacao.Entrada),
            Totais(deHoje, TipoMovimentacao.Saida),
            deHoje.Count(m => m.Tipo == TipoMovimentacao.Ajuste));

        var semana = Enumerable.Range(0, DiasGrafico)
            .Select(i => hoje.AddDays(i - (DiasGrafico - 1)))
            .Select(dia =>
            {
                var doDia = movimentosPeriodo.Where(m => DateOnly.FromDateTime(m.DataHora) == dia).ToList();
                return new MovimentoDia(dia,
                    doDia.Where(m => m.Tipo == TipoMovimentacao.Entrada).Sum(m => m.Quantidade),
                    doDia.Where(m => m.Tipo == TipoMovimentacao.Saida).Sum(m => m.Quantidade));
            })
            .ToList();

        // ---- Alertas e rankings ----
        List<ProdutoResponse> Converter(IEnumerable<Produto> lista, int max) =>
            lista.Take(max).Select(p => ProdutoResponse.De(p, hoje)).ToList();

        var baixo = Converter(EstoqueService.OrdenarPorUrgencia(produtos.Where(p => p.PrecisaReposicao)), MaxAlertas);
        var acima = Converter(produtos.Where(p => p.Status == StatusEstoque.AcimaMaximo)
                                      .OrderByDescending(p => p.Quantidade - p.EstoqueMaximo), MaxAlertas);
        var validade = Converter(produtos.Where(p => p.ObterStatusValidade(hoje) is StatusValidade.Vencido
                                                                                   or StatusValidade.VenceEmBreve)
                                         .OrderBy(p => p.Validade), MaxAlertas);
        var maiores = Converter(produtos.OrderByDescending(p => p.ValorEmEstoque), MaxRanking);

        var corredores = produtos
            .GroupBy(p => p.Corredor)
            .OrderBy(g => g.Key)
            .Select(g => new CorredorResumo(g.Key, g.Count(), g.Sum(p => p.Quantidade),
                                            g.Sum(p => p.ValorEmEstoque), g.Sum(p => p.PesoTotalKg)))
            .ToList();

        var ultimas = await db.Movimentacoes.AsNoTracking()
            .OrderByDescending(m => m.Id)
            .Take(MaxUltimas)
            .ToListAsync();

        return new DashboardResponse(
            EstoqueService.Agora, resumo, porStatus, movimentacoesHoje, semana,
            baixo, acima, validade, maiores, corredores,
            ultimas.Select(MovimentacaoResponse.De).ToList());
    }

    private static TotaisMovimento Totais(List<Movimentacao> lista, TipoMovimentacao tipo)
    {
        var doTipo = lista.Where(m => m.Tipo == tipo).ToList();
        return new TotaisMovimento(doTipo.Count, doTipo.Sum(m => m.Quantidade), doTipo.Sum(m => m.ValorTotal));
    }
}
