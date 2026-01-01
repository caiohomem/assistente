namespace AssistenteExecutivo.Domain.Interfaces;

public interface ITextToSpeechProvider
{
    Task<byte[]> SynthesizeAsync(
        string text,
        string voice,
        string format,
        CancellationToken cancellationToken = default);
}










