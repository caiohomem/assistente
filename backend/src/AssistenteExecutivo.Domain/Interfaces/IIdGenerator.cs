namespace AssistenteExecutivo.Domain.Interfaces;

public interface IIdGenerator
{
    Guid NewGuid();
    string NewId();
}

