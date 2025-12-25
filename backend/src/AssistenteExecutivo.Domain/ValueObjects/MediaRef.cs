using AssistenteExecutivo.Domain.Exceptions;

namespace AssistenteExecutivo.Domain.ValueObjects;

public sealed class MediaRef : ValueObject
{
    public string StorageKey { get; }
    public string Hash { get; }
    public string MimeType { get; }
    public long SizeBytes { get; }

    private MediaRef(string storageKey, string hash, string mimeType, long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new DomainException("Domain:StorageKeyObrigatorio");

        if (string.IsNullOrWhiteSpace(hash))
            throw new DomainException("Domain:HashObrigatorio");

        if (string.IsNullOrWhiteSpace(mimeType))
            throw new DomainException("Domain:MimeTypeObrigatorio");

        if (sizeBytes <= 0)
            throw new DomainException("Domain:TamanhoArquivoInvalido");

        StorageKey = storageKey;
        Hash = hash;
        MimeType = mimeType;
        SizeBytes = sizeBytes;
    }

    public static MediaRef Create(string storageKey, string hash, string mimeType, long sizeBytes)
    {
        return new MediaRef(storageKey, hash, mimeType, sizeBytes);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return StorageKey;
        yield return Hash;
    }
}

