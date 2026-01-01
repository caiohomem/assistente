namespace AssistenteExecutivo.Domain.Interfaces;

public interface IFileStore
{
    Task<string> StoreAsync(byte[] fileBytes, string fileName, string mimeType, CancellationToken cancellationToken = default);
    Task<byte[]> GetAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    Task<string> ComputeHashAsync(byte[] fileBytes, CancellationToken cancellationToken = default);
}












