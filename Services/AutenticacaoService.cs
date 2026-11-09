using Microsoft.EntityFrameworkCore;
using StockWay.Data;
using StockWay.DTOs;
using StockWay.Exceptions;
using StockWay.Models;

namespace StockWay.Services;

/// <summary>Cadastro de usuários e login (confere e-mail e senha e entrega o token).</summary>
public class AutenticacaoService(StockWayContext db, GeradorDeToken tokens, ConfiguracaoAutenticacao config)
{
    private const string MensagemLoginInvalido = "e-mail ou senha inválidos";

    // Quando o e-mail não existe, a senha é conferida contra este hash mesmo assim.
    // Assim a resposta demora o mesmo tempo e não revela quais e-mails estão cadastrados.
    private static readonly Lazy<string> HashFicticio = new(() => HashDeSenha.Gerar("senha-ficticia-0"));

    public async Task<Usuario> CadastrarAsync(CadastroUsuarioRequest dados, bool solicitanteLogado)
    {
        if (!config.CadastroAberto && !solicitanteLogado && await db.Usuarios.AnyAsync())
            throw RegraNegocioException.NaoAutenticado(
                "o cadastro de novos usuários exige login; peça a um usuário já cadastrado para criar a sua conta");

        // O construtor e os setters do Model fazem todas as verificações e geram o hash da senha.
        var usuario = new Usuario(dados.Nome ?? string.Empty, dados.Cargo ?? string.Empty,
                                  dados.Email ?? string.Empty, dados.Senha ?? string.Empty);

        if (await db.Usuarios.AnyAsync(u => u.Email == usuario.Email))
            throw RegraNegocioException.Conflito($"já existe um usuário cadastrado com o e-mail '{usuario.Email}'");

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    public async Task<LoginResponse> EntrarAsync(LoginRequest dados)
    {
        var email = Usuario.NormalizarEmail(dados.Email);
        var senha = dados.Senha ?? string.Empty;
        var usuario = email.Length == 0 ? null : await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

        if (usuario is null)
        {
            HashDeSenha.Verificar(senha, HashFicticio.Value);
            throw RegraNegocioException.NaoAutenticado(MensagemLoginInvalido);
        }
        if (!usuario.ConferirSenha(senha))
            throw RegraNegocioException.NaoAutenticado(MensagemLoginInvalido);
        if (!usuario.Ativo)
            throw RegraNegocioException.Proibido("este usuário está desativado; procure o responsável pelo sistema");

        usuario.RegistrarAcesso();
        await db.SaveChangesAsync();

        var (token, expiraEm) = tokens.Gerar(usuario);
        return new LoginResponse(token, "Bearer", expiraEm, UsuarioResponse.De(usuario));
    }

    /// <summary>Dados do usuário do token. Falha (401) se a conta foi removida ou desativada depois do login.</summary>
    public async Task<Usuario> ObterAtivoAsync(int id) =>
        await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id && u.Ativo)
        ?? throw RegraNegocioException.NaoAutenticado("sessão inválida; faça login novamente");
}
