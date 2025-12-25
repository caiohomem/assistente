namespace AssistenteExecutivo.Domain.ValueObjects;

public sealed class OcrExtract : ValueObject
{
    public string? RawText { get; }
    public string? Name { get; }
    public string? Email { get; }
    public string? Phone { get; }
    public string? Company { get; }
    public string? JobTitle { get; }
    public Dictionary<string, decimal> ConfidenceScores { get; }

    public OcrExtract(
        string? rawText = null,
        string? name = null,
        string? email = null,
        string? phone = null,
        string? company = null,
        string? jobTitle = null,
        Dictionary<string, decimal>? confidenceScores = null)
    {
        RawText = rawText?.Trim();
        Name = name?.Trim();
        Email = email?.Trim();
        Phone = phone?.Trim();
        Company = company?.Trim();
        JobTitle = jobTitle?.Trim();
        ConfidenceScores = confidenceScores ?? new Dictionary<string, decimal>();
    }

    public bool HasMinimumData => !string.IsNullOrWhiteSpace(Email) || !string.IsNullOrWhiteSpace(Phone);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        // RawText is for debugging/audit and is intentionally excluded from equality.
        yield return Name ?? string.Empty;
        yield return Email ?? string.Empty;
        yield return Phone ?? string.Empty;
        yield return Company ?? string.Empty;
        yield return JobTitle ?? string.Empty;
    }
}

