using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using StockWay.Exceptions;

namespace StockWay.Services;

/// <summary>
/// Envia e-mails pela API HTTP do Brevo: POST https://api.brevo.com/v3/smtp/email,
/// com a chave no cabeçalho "api-key". Documentação: https://developers.brevo.com/reference/sendtransacemail
/// </summary>
public class BrevoEmailService(HttpClient http, ConfiguracaoBrevo config, ILogger<BrevoEmailService> logger) : IEnvioDeEmail
{
    // O Brevo usa camelCase ("htmlContent"), diferente do snake_case da nossa API.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<string> EnviarAsync(MensagemDeEmail mensagem, CancellationToken cancelamento = default)
    {
        if (!config.Configurado)
            throw new FalhaNoEnvioDeEmail(
                "o envio de e-mails não está configurado; defina Brevo__ChaveApi e Brevo__RemetenteEmail");
        if (mensagem.Para.Count == 0)
            throw new FalhaNoEnvioDeEmail("nenhum destinatário informado");

        var corpo = new
        {
            sender = new { name = config.RemetenteNome, email = config.RemetenteEmail },
            to = mensagem.Para.Select(d => new { email = d.Email, name = d.Nome }),
            subject = mensagem.Assunto,
            htmlContent = mensagem.Html,
            textContent = mensagem.Texto,
            tags = new[] { "stockway-relatorio-diario" }
        };

        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "smtp/email")
        {
            Content = JsonContent.Create(corpo, options: Json)
        };
        requisicao.Headers.Add("api-key", config.ChaveApi);
        requisicao.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage resposta;
        try
        {
            resposta = await http.SendAsync(requisicao, cancelamento);
        }
        catch (HttpRequestException ex)
        {
            throw new FalhaNoEnvioDeEmail($"não foi possível falar com o Brevo ({ex.Message})");
        }
        catch (TaskCanceledException) when (!cancelamento.IsCancellationRequested)
        {
            throw new FalhaNoEnvioDeEmail($"o Brevo não respondeu em {http.Timeout.TotalSeconds:0} segundos");
        }

        using (resposta)
        {
            var texto = await resposta.Content.ReadAsStringAsync(cancelamento);
            if (!resposta.IsSuccessStatusCode)
                throw new FalhaNoEnvioDeEmail($"o Brevo recusou o envio (HTTP {(int)resposta.StatusCode}): {LerCampo(texto, "message") ?? texto}");

            var id = LerCampo(texto, "messageId") ?? string.Empty;
            logger.LogInformation("E-mail \"{Assunto}\" aceito pelo Brevo para {Quantidade} destinatário(s): {Id}",
                mensagem.Assunto, mensagem.Para.Count, id);
            return id;
        }
    }

    private static string? LerCampo(string json, string campo)
    {
        try
        {
            using var documento = JsonDocument.Parse(json);
            return documento.RootElement.TryGetProperty(campo, out var valor) ? valor.ToString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
