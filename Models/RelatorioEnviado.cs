namespace StockWay.Models;

/// <summary>
/// MODEL RelatorioEnviado: histórico dos envios do relatório diário por e-mail.
/// Serve de auditoria (quem recebeu, quando, se deu certo) e para o agendador saber
/// se o relatório do dia já saiu e de onde o próximo período começa.
/// Imutável, como a Movimentacao: só é criado pelo construtor.
/// </summary>
public class RelatorioEnviado
{
    public const int DestinatariosMax = 2000;
    public const int MensagemMax = 500;

    /// <summary>Usado apenas pelo Entity Framework ao ler do banco.</summary>
    private RelatorioEnviado() { }

    public RelatorioEnviado(TipoEnvioRelatorio tipo, DateTime periodoInicio, DateTime periodoFim,
                            IReadOnlyCollection<string> emails, SituacaoEnvioRelatorio situacao,
                            string? mensagem, string? solicitadoPor)
    {
        Tipo = tipo;
        DataReferencia = DateOnly.FromDateTime(periodoFim);
        PeriodoInicio = periodoInicio;
        PeriodoFim = periodoFim;
        QuantidadeDestinatarios = emails.Count;
        Destinatarios = Cortar(string.Join(", ", emails), DestinatariosMax) ?? string.Empty;
        Situacao = situacao;
        Mensagem = Cortar(mensagem, MensagemMax);
        SolicitadoPor = Cortar(solicitadoPor, Movimentacao.ResponsavelMax);
        CriadoEm = Relogio.Agora;
    }

    public long Id { get; private set; }
    public TipoEnvioRelatorio Tipo { get; private set; }

    /// <summary>Dia a que o relatório se refere (o dia do fim do período).</summary>
    public DateOnly DataReferencia { get; private set; }

    /// <summary>As movimentações contadas vão de PeriodoInicio (incluído) até PeriodoFim (excluído).</summary>
    public DateTime PeriodoInicio { get; private set; }
    public DateTime PeriodoFim { get; private set; }

    /// <summary>E-mails que receberam, separados por vírgula.</summary>
    public string Destinatarios { get; private set; } = string.Empty;
    public int QuantidadeDestinatarios { get; private set; }

    public SituacaoEnvioRelatorio Situacao { get; private set; }

    /// <summary>Identificador do e-mail no Brevo, ou o motivo da falha.</summary>
    public string? Mensagem { get; private set; }

    /// <summary>Nome de quem pediu o envio manual (vazio no envio automático).</summary>
    public string? SolicitadoPor { get; private set; }

    public DateTime CriadoEm { get; private set; }

    private static string? Cortar(string? texto, int maximo) =>
        texto is null ? null : texto.Length > maximo ? texto[..maximo] : texto;
}
