using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StockWay.DTOs;
using StockWay.Exceptions;
using StockWay.Services;

namespace StockWay.Controllers;

/// <summary>Cadastro de usuários, login e dados de quem está logado.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AutenticacaoController(AutenticacaoService servico) : ControllerBase
{
    /// <summary>Limite de tentativas por IP (protege contra quem tenta adivinhar senhas).</summary>
    public const string PoliticaDeLimite = "login";

    /// <summary>Cria um usuário com nome, cargo, e-mail e senha.</summary>
    /// <remarks>Senha: de 8 a 128 caracteres, com pelo menos uma letra e um número.</remarks>
    [HttpPost("cadastro")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticaDeLimite)]
    public async Task<ActionResult<RespostaApi<UsuarioResponse>>> Cadastrar([FromBody] CadastroUsuarioRequest dados)
    {
        var usuario = await servico.CadastrarAsync(dados, solicitanteLogado: User.EstaLogado());
        return StatusCode(StatusCodes.Status201Created, RespostaApi.Ok(UsuarioResponse.De(usuario)));
    }

    /// <summary>Confere e-mail e senha e devolve o token de acesso.</summary>
    /// <remarks>Envie o token nas próximas requisições no cabeçalho "Authorization: Bearer &lt;token&gt;".</remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticaDeLimite)]
    public async Task<ActionResult<RespostaApi<LoginResponse>>> Entrar([FromBody] LoginRequest dados) =>
        Ok(RespostaApi.Ok(await servico.EntrarAsync(dados)));

    /// <summary>Dados do usuário dono do token (útil para o site mostrar nome e cargo).</summary>
    [HttpGet("eu")]
    [Authorize]
    public async Task<ActionResult<RespostaApi<UsuarioResponse>>> UsuarioAtual()
    {
        var id = User.IdDoUsuario() ?? throw RegraNegocioException.NaoAutenticado("sessão inválida; faça login novamente");
        return Ok(RespostaApi.Ok(UsuarioResponse.De(await servico.ObterAtivoAsync(id))));
    }
}
