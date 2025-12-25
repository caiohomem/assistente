using AssistenteExecutivo.Domain.Interfaces;

namespace AssistenteExecutivo.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}


