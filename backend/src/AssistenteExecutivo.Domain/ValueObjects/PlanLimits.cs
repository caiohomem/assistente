namespace AssistenteExecutivo.Domain.ValueObjects;

public class PlanLimits : ValueObject
{
    public int? Contacts { get; private set; }
    public int? Notes { get; private set; }
    public int? CreditsPerMonth { get; private set; }
    public decimal? StorageGB { get; private set; }

    private PlanLimits() { } // EF Core

    public PlanLimits(
        int? contacts = null,
        int? notes = null,
        int? creditsPerMonth = null,
        decimal? storageGB = null)
    {
        Contacts = contacts;
        Notes = notes;
        CreditsPerMonth = creditsPerMonth;
        StorageGB = storageGB;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Contacts ?? 0;
        yield return Notes ?? 0;
        yield return CreditsPerMonth ?? 0;
        yield return StorageGB ?? 0;
    }
}










