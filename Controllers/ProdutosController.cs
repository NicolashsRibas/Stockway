using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using StrockWay.DTOs;
using StrockWay.Models;
using StrockWay.Services;

namespace StrockWay.Controllers;

/// <summary>Cadastro e consulta de produtos.</summary>
[ApiController]
[Route("api/produtos")]
[Produces("application/json")]
public class ProdutosController(EstoqueService servico) : ControllerBase
{
    /// <summary>Lista e pesquisa produtos.</summary>
    /// <param name="busca">Parte do nome ou do código.</param>
    /// <param name="corredor">Corredor do endereço.</param>
    /// <param name="status">ZERADO, BAIXO, NORMAL ou ACIMA_MAXIMO.</param>
    /// <param name="validade">NAO_PERECIVEL, OK, VENCE_EM_BREVE ou VENCIDO.</param>
    /// <param name="ordenar">nome, codigo, quantidade, valor_em_estoque, endereco, validade ou urgencia.</param>
    /// <param name="incluirInativos">Inclui produtos removidos.</param>
    [HttpGet]
    public async Task<ActionResult<RespostaApi<ListaProdutosResponse>>> Listar(
        [FromQuery] string? busca,
        [FromQuery] string? corredor,
        [FromQuery] string? status,
        [FromQuery] string? validade,
        [FromQuery] string? ordenar,
        [FromQuery(Name = "incluir_inativos")] bool incluirInativos = false)
    {
        var filtro = new FiltroProdutos(
            busca, corredor,
            EnumTexto.Ler<StatusEstoque>(status, "status"),
            EnumTexto.Ler<StatusValidade>(validade, "validade"),
            ordenar, incluirInativos);

        var produtos = await servico.ListarProdutosAsync(filtro);
        var hoje = EstoqueService.Hoje;

        return Ok(RespostaApi.Ok(new ListaProdutosResponse(
            produtos.Count,
            produtos.Sum(p => p.ValorEmEstoque),
            produtos.Sum(p => p.PesoTotalKg),
            produtos.Select(p => ProdutoResponse.De(p, hoje)).ToList())));
    }

    /// <summary>Consulta um produto (inclusive removido) com as 10 últimas movimentações.</summary>
    [HttpGet("{codigo}")]
    public async Task<ActionResult<RespostaApi<ProdutoDetalheResponse>>> Obter(string codigo)
    {
        var produto = await servico.ObterProdutoAsync(codigo, incluirInativos: true);
        var ultimas = await servico.UltimasMovimentacoesAsync(produto.Codigo, 10);

        return Ok(RespostaApi.Ok(new ProdutoDetalheResponse(
            ProdutoResponse.De(produto, EstoqueService.Hoje),
            ultimas.Select(MovimentacaoResponse.De).ToList())));
    }

    /// <summary>Cadastra um produto.</summary>
    [HttpPost]
    public async Task<ActionResult<RespostaApi<ProdutoResponse>>> Cadastrar([FromBody] ProdutoCriarRequest dados)
    {
        var produto = await servico.CadastrarAsync(dados);
        return StatusCode(StatusCodes.Status201Created,
            RespostaApi.Ok(ProdutoResponse.De(produto, EstoqueService.Hoje)));
    }

    /// <summary>Altera dados cadastrais (envie só os campos que mudaram). A quantidade não é alterada aqui.</summary>
    [HttpPut("{codigo}")]
    public async Task<ActionResult<RespostaApi<ProdutoResponse>>> Atualizar(string codigo,
        [FromBody] ProdutoAtualizarRequest dados)
    {
        var produto = await servico.AtualizarAsync(codigo, dados);
        return Ok(RespostaApi.Ok(ProdutoResponse.De(produto, EstoqueService.Hoje)));
    }

    /// <summary>Remove o produto (exclusão lógica; só com saldo zero).</summary>
    /// <remarks>Responsável e motivo podem vir no corpo JSON ou na query string.</remarks>
    [HttpDelete("{codigo}")]
    public async Task<ActionResult<RespostaApi<object>>> Remover(string codigo,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RemoverProdutoRequest? dados,
        [FromQuery] string? responsavel,
        [FromQuery] string? motivo)
    {
        await servico.RemoverAsync(codigo, dados?.Responsavel ?? responsavel, dados?.Motivo ?? motivo);
        return Ok(RespostaApi.Ok<object>(new { mensagem = "produto removido com sucesso" }));
    }

    /// <summary>Histórico de movimentações do produto.</summary>
    [HttpGet("{codigo}/movimentacoes")]
    public async Task<ActionResult<RespostaApi<PaginaMovimentacoesResponse>>> Historico(string codigo,
        [FromQuery] string? tipo,
        [FromQuery(Name = "data_inicio")] DateOnly? dataInicio,
        [FromQuery(Name = "data_fim")] DateOnly? dataFim,
        [FromQuery] int limite = 50,
        [FromQuery] int offset = 0)
    {
        var produto = await servico.ObterProdutoAsync(codigo, incluirInativos: true);
        var filtro = MovimentacoesController.CriarFiltro(produto.Codigo, tipo, dataInicio, dataFim, limite, offset);
        var (total, itens) = await servico.ListarMovimentacoesAsync(filtro);
        return Ok(RespostaApi.Ok(new PaginaMovimentacoesResponse(
            itens.Select(MovimentacaoResponse.De).ToList(), total, filtro.Limite, filtro.Offset)));
    }
}
