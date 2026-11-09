using System.Globalization;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using StockWay.Models;

namespace StockWay.Services;

/// <summary>Gera o token JWT entregue no login. O site manda esse token no cabeçalho
/// "Authorization: Bearer &lt;token&gt;" em todas as requisições.</summary>
public class GeradorDeToken(ConfiguracaoAutenticacao config)
{
    private readonly JsonWebTokenHandler _handler = new();

    public (string Token, DateTime ExpiraEm) Gerar(Usuario usuario)
    {
        var agora = DateTime.UtcNow;
        var expiraEm = agora.AddMinutes(config.ExpiracaoMinutos);

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = config.Emissor,
            Audience = config.Emissor,
            IssuedAt = agora,
            NotBefore = agora,
            Expires = expiraEm,
            Subject = new ClaimsIdentity(
            [
                new Claim(UsuarioLogado.ClaimId, usuario.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim(UsuarioLogado.ClaimNome, usuario.Nome),
                new Claim(UsuarioLogado.ClaimEmail, usuario.Email),
                new Claim(UsuarioLogado.ClaimCargo, usuario.Cargo)
            ]),
            SigningCredentials = new SigningCredentials(config.ChaveDeAssinatura, SecurityAlgorithms.HmacSha256)
        };

        return (_handler.CreateToken(descritor), expiraEm);
    }
}
