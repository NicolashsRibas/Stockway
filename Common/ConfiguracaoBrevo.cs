namespace StockWay.Common;

/// <summary>
/// Configuração do Brevo (api.brevo.com), o serviço externo que entrega os e-mails.
/// Seção "Brevo" do appsettings.json; a chave vem da variável de ambiente Brevo__ChaveApi.
/// </summary>
public class ConfiguracaoBrevo
{
    public const string Secao = "Brevo";

    /// <summary>Endereço da API do Brevo (só muda em testes).</summary>
    public string Url { get; set; } = "https://api.brevo.com/v3/";

    /// <summary>Chave de API criada no painel do Brevo (SMTP &amp; API &gt; API Keys). Nunca no appsettings.json.</summary>
    public string? ChaveApi { get; set; }

    /// <summary>E-mail que aparece como remetente. Precisa estar verificado no Brevo (Senders).</summary>
    public string? RemetenteEmail { get; set; }

    public string RemetenteNome { get; set; } = "StockWay";

    public bool Configurado => !string.IsNullOrWhiteSpace(ChaveApi) && !string.IsNullOrWhiteSpace(RemetenteEmail);

    public static ConfiguracaoBrevo Carregar(IConfiguration configuracao)
    {
        var config = configuracao.GetSection(Secao).Get<ConfiguracaoBrevo>() ?? new ConfiguracaoBrevo();

        if (!Uri.TryCreate(config.Url, UriKind.Absolute, out var url) || url.Scheme is not ("http" or "https"))
            throw new InvalidOperationException("Brevo:Url precisa ser um endereço http(s) válido.");
        if (!config.Url.EndsWith('/'))
            config.Url += "/";

        return config;
    }
}
