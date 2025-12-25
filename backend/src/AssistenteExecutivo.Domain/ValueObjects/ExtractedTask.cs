namespace AssistenteExecutivo.Domain.ValueObjects;

public sealed class ExtractedTask : ValueObject
{
    public string Description { get; }
    public DateTime? DueDate { get; }
    public string? Priority { get; }

    public ExtractedTask(string description, DateTime? dueDate = null, string? priority = null)
    {
        Description = description ?? string.Empty;
        DueDate = dueDate;
        Priority = priority;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Description;
        yield return DueDate ?? DateTime.MinValue;
        yield return Priority ?? string.Empty;
    }
}

