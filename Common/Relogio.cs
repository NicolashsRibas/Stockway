namespace StockWay.Common;

/// <summary>Data e hora do sistema em um só lugar (usado pelos Models e Services).</summary>
public static class Relogio
{
    /// <summary>Data de hoje (sem horário).</summary>
    public static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>
    /// Data/hora local atual, sem frações de segundo.
    /// O Kind fica "Unspecified" (igual ao que o banco devolve), para o JSON sair sempre no
    /// mesmo formato ("2026-11-07T14:30:00"): logo depois de gravar e ao ler do banco.
    /// </summary>
    public static DateTime Agora
    {
        get
        {
            var agora = DateTime.Now;
            return new DateTime(agora.Ticks - agora.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Unspecified);
        }
    }
}
