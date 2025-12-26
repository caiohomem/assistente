using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.Interfaces;

/// <summary>
/// Serviço para refinar a associação de campos extraídos do OCR usando LLM
/// </summary>
public interface IOcrFieldRefinementService
{
    /// <summary>
    /// Refina a associação de campos ao texto extraído usando LLM
    /// </summary>
    /// <param name="rawText">Texto bruto extraído pelo OCR</param>
    /// <param name="initialExtract">Extração inicial (opcional) para preservar campos já corretos</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>OcrExtract refinado com melhor associação de campos</returns>
    Task<OcrExtract> RefineFieldsAsync(
        string rawText,
        OcrExtract? initialExtract = null,
        CancellationToken cancellationToken = default);
}

