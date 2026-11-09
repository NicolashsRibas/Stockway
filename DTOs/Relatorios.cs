using StockWay.Models;

namespace StockWay.DTOs;

// Formato do relatório diário (GET /api/relatorios/diario) e do histórico de envios.

public record TotaisDoPeriodo(int Registros, int Unidades);

public record MovimentoDoProduto(string Codigo, string Nome, int UnidadesEntrada, int UnidadesSaida);

public record RelatorioDiarioResponse(
    DateOnly Data,
    DateTime PeriodoInicio,
    DateTime PeriodoFim,
    TotaisDoPeriodo Entradas,
    TotaisDoPeriodo Saidas,
    List<MovimentoDoProduto> MovimentacoesPorProduto,
    int DiasAvisoValidade,
    List<ProdutoResponse> VencemEmBreve,
    List<ProdutoResponse> NoEstoqueMinimo);

public record EnvioRelatorioResponse(
    long Id,
    TipoEnvioRelatorio Tipo,
    DateOnly DataReferencia,
    DateTime PeriodoInicio,
    DateTime PeriodoFim,
    int QuantidadeDestinatarios,
    string Destinatarios,
    SituacaoEnvioRelatorio Situacao,
    string? Mensagem,
    string? SolicitadoPor,
    DateTime CriadoEm)
{
    public static EnvioRelatorioResponse De(RelatorioEnviado r) => new(
        r.Id, r.Tipo, r.DataReferencia, r.PeriodoInicio, r.PeriodoFim, r.QuantidadeDestinatarios,
        r.Destinatarios, r.Situacao, r.Mensagem, r.SolicitadoPor, r.CriadoEm);
}
