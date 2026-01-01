using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Infrastructure.Services;

/// <summary>
/// Serviço para cortar arquivos de áudio para caber no limite de 25MB da API OpenAI Whisper
/// </summary>
public class AudioTrimmer
{
    private readonly ILogger<AudioTrimmer> _logger;
    private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    public AudioTrimmer(ILogger<AudioTrimmer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Corta o áudio para caber em 25MB, mantendo o início do arquivo
    /// </summary>
    /// <param name="audioBytes">Bytes do arquivo de áudio original</param>
    /// <param name="mimeType">Tipo MIME do arquivo</param>
    /// <returns>Tupla com os bytes cortados e um flag indicando se foi cortado</returns>
    public (byte[] trimmedBytes, bool wasTrimmed) TrimToMaxSize(byte[] audioBytes, string mimeType)
    {
        if (audioBytes.Length <= MaxFileSizeBytes)
        {
            return (audioBytes, false);
        }

        var originalSizeMB = audioBytes.Length / (1024.0 * 1024.0);
        _logger.LogWarning(
            "Arquivo de áudio excede o limite de 25MB. Tamanho original: {OriginalSizeMB:F2} MB. " +
            "Cortando para manter os primeiros 25MB (início da gravação).",
            originalSizeMB);

        // Para a maioria dos formatos de áudio, podemos simplesmente pegar os primeiros 25MB
        // A API OpenAI Whisper é tolerante e pode processar arquivos parcialmente corrompidos
        // Mantemos uma margem de segurança de 1KB para evitar problemas de boundary
        var trimmedSize = (int)(MaxFileSizeBytes - 1024);
        var trimmedBytes = new byte[trimmedSize];
        Array.Copy(audioBytes, 0, trimmedBytes, 0, trimmedSize);

        var trimmedSizeMB = trimmedBytes.Length / (1024.0 * 1024.0);
        _logger.LogInformation(
            "Áudio cortado com sucesso. Tamanho após corte: {TrimmedSizeMB:F2} MB (original: {OriginalSizeMB:F2} MB). " +
            "Apenas o início da gravação será processado.",
            trimmedSizeMB, originalSizeMB);

        return (trimmedBytes, true);
    }

    /// <summary>
    /// Obtém uma mensagem informativa sobre o corte do áudio
    /// </summary>
    public string GetTrimmedMessage(byte[] originalBytes, bool wasTrimmed)
    {
        if (!wasTrimmed)
        {
            return string.Empty;
        }

        var originalSizeMB = originalBytes.Length / (1024.0 * 1024.0);
        var trimmedSizeMB = MaxFileSizeBytes / (1024.0 * 1024.0);

        return $"⚠️ Arquivo de áudio grande ({originalSizeMB:F2} MB) foi automaticamente cortado para {trimmedSizeMB:F2} MB. " +
               $"Apenas o início da gravação será processado. Para processar o áudio completo, considere dividir em partes menores.";
    }
}







