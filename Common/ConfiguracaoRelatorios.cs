using System.Globalization;
using System.Text;

namespace StockWay.Common;

/// <summary>Configuração do relatório diário por e-mail (seção "Relatorios" do appsettings.json).</summary>
public class ConfiguracaoRelatorios
{
    public const string Secao = "Relatorios";

    /// <summary>false = o agendador não envia nada (a rota manual continua funcionando).</summary>
    public bool EnvioAutomatico { get; set; } = true;

    /// <summary>Horário do envio automático, no formato HH:mm (horário do servidor).</summary>
    public string Horario { get; set; } = "18:00";

    /// <summary>Produtos que vencem em até N dias entram no relatório (padrão: 7, uma semana).</summary>
    public int DiasAvisoValidade { get; set; } = 7;

    /// <summary>Recebem o relatório os usuários ativos cujo cargo contém este texto
    /// (sem diferenciar maiúsculas nem acentos): "Gerente", "gerente de estoque"...</summary>
    public string CargoDestinatario { get; set; } = "gerente";

    public TimeOnly HorarioDeEnvio { get; private set; }

    public static ConfiguracaoRelatorios Carregar(IConfiguration configuracao)
    {
        var config = configuracao.GetSection(Secao).Get<ConfiguracaoRelatorios>() ?? new ConfiguracaoRelatorios();

        if (!TimeOnly.TryParseExact(config.Horario, ["HH:mm", "H:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var horario))
            throw new InvalidOperationException("Relatorios:Horario precisa estar no formato HH:mm, por exemplo \"18:00\".");
        if (config.DiasAvisoValidade is < 1 or > 90)
            throw new InvalidOperationException("Relatorios:DiasAvisoValidade deve ficar entre 1 e 90.");
        if (string.IsNullOrWhiteSpace(config.CargoDestinatario))
            throw new InvalidOperationException("Relatorios:CargoDestinatario não pode ficar vazio.");

        config.HorarioDeEnvio = horario;
        return config;
    }

    /// <summary>Verdadeiro se o cargo é de quem recebe o relatório (ex.: contém "gerente").</summary>
    public bool EhDestinatario(string? cargo) =>
        Normalizar(cargo).Contains(Normalizar(CargoDestinatario), StringComparison.Ordinal);

    private static string Normalizar(string? texto)
    {
        var decomposto = (texto ?? string.Empty).Trim().Normalize(NormalizationForm.FormD);
        var semAcento = new StringBuilder(decomposto.Length);
        foreach (var c in decomposto)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                semAcento.Append(c);
        return semAcento.ToString().ToLowerInvariant();
    }
}
