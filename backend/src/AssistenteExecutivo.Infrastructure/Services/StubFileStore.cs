using AssistenteExecutivo.Domain.Interfaces;
using System.Security.Cryptography;

namespace AssistenteExecutivo.Infrastructure.Services;

/// <summary>
/// Stub implementation of IFileStore.
/// This is a placeholder that stores files in memory (not persistent).
/// Replace with actual implementation (e.g., Azure Blob Storage, AWS S3, local file system) when ready.
/// 
/// WARNING: Files stored here are lost when the application restarts.
/// </summary>
public class StubFileStore : IFileStore
{
    private readonly Dictionary<string, byte[]> _storage = new();
    private static readonly object _lock = new();

    public Task<string> StoreAsync(byte[] fileBytes, string fileName, string mimeType, CancellationToken cancellationToken = default)
    {
        // Stub implementation: stores in memory dictionary
        // TODO: Replace with actual file storage service integration
        var key = $"stub/{Guid.NewGuid()}/{fileName}";
        _storage[key] = fileBytes;
        return Task.FromResult(key);
    }

    public Task<byte[]> GetAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        // Stub implementation: retrieves from memory dictionary
        return Task.FromResult(_storage.TryGetValue(storageKey, out var bytes) ? bytes : Array.Empty<byte>());
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        // Stub implementation: removes from memory dictionary
        _storage.Remove(storageKey);
        return Task.CompletedTask;
    }

    public Task<string> ComputeHashAsync(byte[] fileBytes, CancellationToken cancellationToken = default)
    {
        // Compute SHA256 hash
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(fileBytes);
        var hashString = Convert.ToHexString(hash).ToLowerInvariant();
        return Task.FromResult(hashString);
    }
}


