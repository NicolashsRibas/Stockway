using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace StockWay.Common;

/// <summary>
/// Configuração do login, lida da seção "Autenticacao" do appsettings.json
/// (ou de variáveis de ambiente, ex.: Autenticacao__Chave).
/// </summary>
public class ConfiguracaoAutenticacao
{
    public const string Secao = "Autenticacao";
    public const int TamanhoMinimoChave = 32;

    /// <summary>true = todas as rotas exigem token, menos login, cadastro e health.</summary>
    public bool ExigirLogin { get; set; }

    /// <summary>true = qualquer pessoa pode criar conta. false = só um usuário logado cria contas novas
    /// (o primeiro usuário pode sempre ser criado, senão ninguém conseguiria entrar).</summary>
    public bool CadastroAberto { get; set; } = true;

    /// <summary>Validade do token, em minutos (padrão: 8 horas, um turno de trabalho).</summary>
    public int ExpiracaoMinutos { get; set; } = 480;

    /// <summary>Quem emite e quem aceita o token.</summary>
    public string Emissor { get; set; } = "StockWay";

    /// <summary>Segredo que assina os tokens (mínimo de 32 caracteres). Nunca coloque no appsettings.json:
    /// use a variável de ambiente Autenticacao__Chave.</summary>
    public string? Chave { get; set; }

    /// <summary>Chave usada para assinar e conferir os tokens.</summary>
    public SymmetricSecurityKey ChaveDeAssinatura { get; private set; } = null!;

    /// <summary>Verdadeiro quando nenhuma chave foi configurada e uma aleatória foi gerada ao iniciar.</summary>
    public bool ChaveTemporaria { get; private set; }

    public static ConfiguracaoAutenticacao Carregar(IConfiguration configuracao)
    {
        var config = configuracao.GetSection(Secao).Get<ConfiguracaoAutenticacao>() ?? new ConfiguracaoAutenticacao();

        if (config.ExpiracaoMinutos < 5 || config.ExpiracaoMinutos > 60 * 24 * 30)
            throw new InvalidOperationException("Autenticacao:ExpiracaoMinutos deve ficar entre 5 e 43200 (30 dias).");
        if (string.IsNullOrWhiteSpace(config.Emissor))
            throw new InvalidOperationException("Autenticacao:Emissor não pode ficar vazio.");

        byte[] bytes;
        if (string.IsNullOrWhiteSpace(config.Chave))
        {
            // Sem chave configurada: gera uma aleatória. Funciona, mas os tokens deixam de valer
            // sempre que a API reinicia (todos precisam entrar de novo).
            bytes = RandomNumberGenerator.GetBytes(64);
            config.ChaveTemporaria = true;
        }
        else
        {
            bytes = Encoding.UTF8.GetBytes(config.Chave);
            if (bytes.Length < TamanhoMinimoChave)
                throw new InvalidOperationException(
                    $"Autenticacao:Chave precisa ter pelo menos {TamanhoMinimoChave} caracteres.");
        }

        config.ChaveDeAssinatura = new SymmetricSecurityKey(bytes);
        return config;
    }
}
