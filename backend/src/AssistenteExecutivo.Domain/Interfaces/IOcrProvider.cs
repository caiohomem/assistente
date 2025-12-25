using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.Interfaces;

public interface IOcrProvider
{
    Task<OcrExtract> ExtractFieldsAsync(byte[] imageBytes, string mimeType, CancellationToken cancellationToken = default);
}


