using Microsoft.AspNetCore.Mvc;
using StrockWay.DTOs;
using StrockWay.Exceptions;
using StrockWay.Models;
using StrockWay.Services;

namespace StrockWay.Controllers;

/// <summary>Identificação de produtos em situação de alerta.</summary>
[ApiController]
[Route("api/estoque")]
[Produces("application/json")]
public class EstoqueController(EstoqueService servico) : ControllerBase
{
    /// <summary>Produtos zerados ou abaixo do mínimo, mais urgentes primeiro.</summary>
    [HttpGet("baixo")]
    public async Task<ActionResult<RespostaApi<BaixoEstoqueResponse>>> Baixo()
    {
        var hoje = EstoqueService.Hoje;
        var produtos = await ListarAtivosAsync();
        var baixos = EstoqueService.OrdenarPorUrgencia(produtos.Where(p => p.PrecisaReposicao)).ToList();

        return Ok(RespostaApi.Ok(new BaixoEstoqueResponse(
            baixos.Count,
            baixos.Sum(p => p.QuantidadeParaRepor * p.ValorUnitario),
            baixos.Select(p => ProdutoResponse.De(p, hoje)).ToList())));
    }

    /// <summary>Todos os alertas: baixo estoque, acima do máximo e validade.</summary>
    [HttpGet("alertas")]
    public async Task<ActionResult<RespostaApi<AlertasResponse>>> Alertas()
    {
        var hoje = EstoqueService.Hoje;
        var produtos = await ListarAtivosAsync();

        List<ProdutoResponse> Converter(IEnumerable<Produto> lista) =>
            lista.Select(p => ProdutoResponse.De(p, hoje)).ToList();

        return Ok(RespostaApi.Ok(new AlertasResponse(
            Converter(EstoqueService.OrdenarPorUrgencia(produtos.Where(p => p.PrecisaReposicao))),
            Converter(produtos.Where(p => p.Status == StatusEstoque.AcimaMaximo)),
            Converter(produtos.Where(p => p.ObterStatusValidade(hoje) is StatusValidade.Vencido
                                                                       or StatusValidade.VenceEmBreve)
                              .OrderBy(p => p.Validade)))));
    }

    /// <summary>Produtos vencidos ou que vencem nos próximos N dias.</summary>
    /// <param name="dias">Janela em dias (0 a 3650, padrão 30).</param>
    [HttpGet("validade")]
    public async Task<ActionResult<RespostaApi<ValidadeResponse>>> Validade([FromQuery] int dias = Produto.DiasAlertaValidade)
    {
        if (dias < 0 || dias > 3650)
            throw RegraNegocioException.Validacao("parâmetro 'dias' deve ser um inteiro entre 0 e 3650");

        var hoje = EstoqueService.Hoje;
        var produtos = (await ListarAtivosAsync())
            .Where(p => p.DiasParaVencer(hoje) is int restante && restante <= dias)
            .OrderBy(p => p.Validade)
            .ToList();

        return Ok(RespostaApi.Ok(new ValidadeResponse(
            dias,
            produtos.Count,
            produtos.Count(p => p.ObterStatusValidade(hoje) == StatusValidade.Vencido),
            produtos.Select(p => ProdutoResponse.De(p, hoje)).ToList())));
    }

    private Task<List<Produto>> ListarAtivosAsync() =>
        servico.ListarProdutosAsync(new FiltroProdutos(null, null, null, null, null, false));
}
