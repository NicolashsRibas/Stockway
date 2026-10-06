using Microsoft.AspNetCore.Mvc;
using StrockWay.DTOs;
using StrockWay.Exceptions;
using StrockWay.Models;
using StrockWay.Services;

namespace StrockWay.Controllers;

/// <summary>Entradas, saídas, ajustes de inventário e log de movimentações.</summary>
[ApiController]
[Route("api/movimentacoes")]
[Produces("application/json")]
public class MovimentacoesController(EstoqueService servico) : ControllerBase
{
    /// <summary>Log de movimentações (mais recentes primeiro).</summary>
    /// <param name="codigo">Código do produto.</param>
    /// <param name="tipo">CADASTRO, ENTRADA, SAIDA, AJUSTE, EDICAO ou REMOCAO.</param>
    /// <param name="dataInicio">AAAA-MM-DD</param>
    /// <param name="dataFim">AAAA-MM-DD</param>
    /// <param name="limite">1 a 1000 (padrão 50).</param>
    /// <param name="offset">Quantos registros pular (paginação).</param>
    [HttpGet]
    public async Task<ActionResult<RespostaApi<PaginaMovimentacoesResponse>>> Listar(
        [FromQuery] string? codigo,
        [FromQuery] string? tipo,
        [FromQuery(Name = "data_inicio")] DateOnly? dataInicio,
        [FromQuery(Name = "data_fim")] DateOnly? dataFim,
        [FromQuery] int limite = 50,
        [FromQuery] int offset = 0)
    {
        var filtro = CriarFiltro(codigo, tipo, dataInicio, dataFim, limite, offset);
        var (total, itens) = await servico.ListarMovimentacoesAsync(filtro);
        return Ok(RespostaApi.Ok(new PaginaMovimentacoesResponse(
            itens.Select(MovimentacaoResponse.De).ToList(), total, filtro.Limite, filtro.Offset)));
    }

    /// <summary>Registra uma ENTRADA (recebimento). Informe valor_unitario para recalcular o custo médio.</summary>
    [HttpPost("entrada")]
    public async Task<ActionResult<RespostaApi<ResultadoMovimentacaoResponse>>> Entrada([FromBody] EntradaRequest dados)
    {
        var (mov, produto) = await servico.RegistrarEntradaAsync(dados);
        return Criado(mov, produto);
    }

    /// <summary>Registra uma SAÍDA (retirada). Recusa se a quantidade for maior que o saldo.</summary>
    [HttpPost("saida")]
    public async Task<ActionResult<RespostaApi<ResultadoMovimentacaoResponse>>> Saida([FromBody] SaidaRequest dados)
    {
        var (mov, produto) = await servico.RegistrarSaidaAsync(dados);
        return Criado(mov, produto);
    }

    /// <summary>AJUSTE de inventário: define o saldo como a quantidade contada (observação obrigatória).</summary>
    [HttpPost("ajuste")]
    public async Task<ActionResult<RespostaApi<ResultadoMovimentacaoResponse>>> Ajuste([FromBody] AjusteRequest dados)
    {
        var (mov, produto) = await servico.RegistrarAjusteAsync(dados);
        return Criado(mov, produto);
    }

    // ------------------------------------------------------------------

    internal static FiltroMovimentacoes CriarFiltro(string? codigo, string? tipo, DateOnly? dataInicio,
        DateOnly? dataFim, int limite, int offset)
    {
        if (limite < 1 || limite > 1000)
            throw RegraNegocioException.Validacao("parâmetro 'limite' deve ser um inteiro entre 1 e 1000");
        if (offset < 0)
            throw RegraNegocioException.Validacao("parâmetro 'offset' deve ser maior ou igual a zero");
        if (dataInicio is not null && dataFim is not null && dataInicio > dataFim)
            throw RegraNegocioException.Validacao("'data_inicio' não pode ser maior que 'data_fim'");

        return new FiltroMovimentacoes(codigo, EnumTexto.Ler<TipoMovimentacao>(tipo, "tipo"),
            dataInicio, dataFim, limite, offset);
    }

    private ObjectResult Criado(Movimentacao mov, Produto produto)
    {
        var hoje = EstoqueService.Hoje;
        var alertas = new List<string>();

        switch (produto.Status)
        {
            case StatusEstoque.Zerado:
                alertas.Add("produto sem estoque");
                break;
            case StatusEstoque.Baixo:
                alertas.Add($"produto atingiu o estoque mínimo; reposição sugerida de {produto.QuantidadeParaRepor} unidade(s)");
                break;
            case StatusEstoque.AcimaMaximo:
                alertas.Add("produto acima do estoque máximo");
                break;
        }
        switch (produto.ObterStatusValidade(hoje))
        {
            case StatusValidade.Vencido:
                alertas.Add("produto com validade vencida");
                break;
            case StatusValidade.VenceEmBreve:
                alertas.Add($"produto vence em {produto.DiasParaVencer(hoje)} dia(s)");
                break;
        }

        return StatusCode(StatusCodes.Status201Created, RespostaApi.Ok(new ResultadoMovimentacaoResponse(
            MovimentacaoResponse.De(mov), ProdutoResponse.De(produto, hoje), alertas)));
    }
}
