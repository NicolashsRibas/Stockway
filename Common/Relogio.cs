namespace StrockWay.Common;

/// <summary>Data e hora do sistema em um só lugar (usado pelos Models e Services).</summary>
public static class Relogio
{
    /// <summary>Data de hoje (sem horário).</summary>
    public static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>Data/hora atual sem frações de segundo.</summary>
    public static DateTime Agora
    {
        get
        {
            var agora = DateTime.Now;
            return new DateTime(agora.Ticks - agora.Ticks % TimeSpan.TicksPerSecond, agora.Kind);
        }
    }
}
