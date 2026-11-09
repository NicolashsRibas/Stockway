namespace StockWay.Services;

/// <summary>Quem recebe o e-mail.</summary>
public record Destinatario(string Email, string Nome);

/// <summary>Um e-mail pronto para envio: HTML para os leitores de e-mail e texto puro como alternativa.</summary>
public record MensagemDeEmail(IReadOnlyList<Destinatario> Para, string Assunto, string Html, string Texto);

/// <summary>
/// Envio de e-mail por um serviço externo. A implementação atual é o BrevoEmailService;
/// trocar de provedor (SendGrid, Resend...) é só criar outra classe com esta interface.
/// </summary>
public interface IEnvioDeEmail
{
    /// <summary>Envia e devolve o identificador da mensagem no provedor. Lança FalhaNoEnvioDeEmail se não conseguir.</summary>
    Task<string> EnviarAsync(MensagemDeEmail mensagem, CancellationToken cancelamento = default);
}
