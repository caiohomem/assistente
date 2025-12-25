using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.Entities;

public class MediaAsset
{
    private MediaAsset() { } // EF Core

    public MediaAsset(
        Guid mediaId,
        Guid ownerUserId,
        MediaRef mediaRef,
        MediaKind kind,
        IClock clock)
    {
        if (mediaId == Guid.Empty)
            throw new DomainException("Domain:MediaIdObrigatorio");

        if (ownerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (mediaRef == null)
            throw new DomainException("Domain:MediaRefObrigatorio");

        if (clock == null)
            throw new DomainException("Domain:ClockObrigatorio");

        MediaId = mediaId;
        OwnerUserId = ownerUserId;
        MediaRef = mediaRef;
        Kind = kind;
        CreatedAt = clock.UtcNow;
    }

    public Guid MediaId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public MediaRef MediaRef { get; private set; } = null!;
    public MediaKind Kind { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Dictionary<string, string> Metadata { get; private set; } = new();
    public byte[]? FileContent { get; private set; } // Conteúdo do arquivo armazenado no banco de dados

    public void SetFileContent(byte[] fileContent)
    {
        if (fileContent == null || fileContent.Length == 0)
            throw new DomainException("Domain:FileContentNaoPodeSerVazio");

        FileContent = fileContent;
    }
}

