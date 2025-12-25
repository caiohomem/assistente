namespace AssistenteExecutivo.Domain.Interfaces;

public interface IClock
{
    DateTime UtcNow { get; }
}

