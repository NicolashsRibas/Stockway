using StrockWay.Models;

namespace StrockWay.DTOs;

// Dados ENVIADOS pela API: é a camada "View" do MVC (o formato do JSON de resposta).

public record EnderecoResponse(string Corredor, string Prateleira, string Descricao);

public record ProdutoResponse(
    int Id,
    string Codigo,
    string Nome,
    int Quantidade,
    decimal ValorUnitario,
    decimal ValorEmEstoque,
    decimal PesoKg,
    decimal PesoTotalKg,
    DateOnly? Validade,
    StatusValidade StatusValidade,
    int? DiasParaVencer,
    EnderecoResponse Endereco,
    int EstoqueMinimo,
    int EstoqueMaximo,
    StatusEstoque Status,
    bool BaixoEstoque,
    bool AcimaMaximo,
    int QuantidadeParaRepor,
    double PercentualOcupacao,
    bool Ativo,
    DateTime CriadoEm,
    DateTime AtualizadoEm)
{
    public static ProdutoResponse De(Produto p, DateOnly hoje) => new(
        p.Id, p.Codigo, p.Nome, p.Quantidade, p.ValorUnitario, p.ValorEmEstoque,
        p.PesoKg, p.PesoTotalKg, p.Validade, p.ObterStatusValidade(hoje), p.DiasParaVencer(hoje),
        new EnderecoResponse(p.Corredor, p.Prateleira, p.EnderecoDescricao),
        p.EstoqueMinimo, p.EstoqueMaximo, p.Status, p.PrecisaReposicao,
        p.Status == StatusEstoque.AcimaMaximo, p.QuantidadeParaRepor, p.PercentualOcupacao,
        p.Ativo, p.CriadoEm, p.AtualizadoEm);
}

public record ListaProdutosResponse(
    int Total,
    decimal ValorTotalEmEstoque,
    decimal PesoTotalKg,
    List<ProdutoResponse> Itens);

public record MovimentacaoResponse(
    long Id,
    TipoMovimentacao Tipo,
    string TipoDescricao,
    int ProdutoId,
    string ProdutoCodigo,
    string ProdutoNome,
    int Quantidade,
    int SaldoAnterior,
    int SaldoAtual,
    decimal ValorUnitario,
    decimal ValorTotal,
    string Responsavel,
    string? Observacao,
    DateTime DataHora)
{
    public static MovimentacaoResponse De(Movimentacao m) => new(
        m.Id, m.Tipo, Movimentacao.Descricao(m.Tipo), m.ProdutoId, m.ProdutoCodigo, m.ProdutoNome,
        m.Quantidade, m.SaldoAnterior, m.SaldoAtual, m.ValorUnitario, m.ValorTotal,
        m.Responsavel, m.Observacao, m.DataHora);
}

public record PaginaMovimentacoesResponse(
    List<MovimentacaoResponse> Itens,
    int Total,
    int Limite,
    int Offset);

public record ProdutoDetalheResponse(
    ProdutoResponse Produto,
    List<MovimentacaoResponse> UltimasMovimentacoes);

/// <summary>Resultado de entrada/saída/ajuste, com alertas para o site exibir.</summary>
public record ResultadoMovimentacaoResponse(
    MovimentacaoResponse Movimentacao,
    ProdutoResponse Produto,
    List<string> Alertas);

public record BaixoEstoqueResponse(
    int Total,
    decimal CustoEstimadoReposicao,
    List<ProdutoResponse> Itens);

public record AlertasResponse(
    List<ProdutoResponse> BaixoEstoque,
    List<ProdutoResponse> AcimaMaximo,
    List<ProdutoResponse> Validade);

public record ValidadeResponse(
    int Dias,
    int Total,
    int Vencidos,
    List<ProdutoResponse> Itens);

// ---------------- Dashboard ----------------

public record ResumoDashboard(
    int TotalProdutos,
    int TotalUnidades,
    decimal ValorTotalEstoque,
    decimal PesoTotalKg,
    int ProdutosBaixoEstoque,
    int ProdutosVencidos,
    int ProdutosVencendoEmBreve,
    int TotalMovimentacoes);

public record EstoquePorStatus(int Zerado, int Baixo, int Normal, int AcimaMaximo);

public record TotaisMovimento(int Registros, int Unidades, decimal Valor);

public record MovimentacoesHoje(TotaisMovimento Entradas, TotaisMovimento Saidas, int Ajustes);

public record MovimentoDia(DateOnly Data, int Entradas, int Saidas);

public record CorredorResumo(string Corredor, int Produtos, int Unidades, decimal Valor, decimal PesoKg);

public record DashboardResponse(
    DateTime GeradoEm,
    ResumoDashboard Resumo,
    EstoquePorStatus EstoquePorStatus,
    MovimentacoesHoje MovimentacoesHoje,
    List<MovimentoDia> MovimentacoesSemana,
    List<ProdutoResponse> AlertasBaixoEstoque,
    List<ProdutoResponse> AlertasAcimaMaximo,
    List<ProdutoResponse> AlertasValidade,
    List<ProdutoResponse> MaioresValoresEmEstoque,
    List<CorredorResumo> EstoquePorCorredor,
    List<MovimentacaoResponse> UltimasMovimentacoes);

public record HealthResponse(string Status, int Produtos, int Movimentacoes, DateTime HoraServidor);
