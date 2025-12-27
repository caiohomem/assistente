using AssistenteExecutivo.Domain.Interfaces;

namespace AssistenteExecutivo.Infrastructure.Services;

/// <summary>
/// Implementation of IIdGenerator using System.Guid.
/// This generates standard GUIDs for entity IDs.
/// </summary>
public class GuidIdGenerator : IIdGenerator
{
    public Guid NewGuid() => Guid.NewGuid();

    public string NewId() => Guid.NewGuid().ToString();
}





