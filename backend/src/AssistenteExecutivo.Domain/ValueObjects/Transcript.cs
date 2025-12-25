namespace AssistenteExecutivo.Domain.ValueObjects;

public sealed class Transcript : ValueObject
{
    public string Text { get; }
    public List<TranscriptSegment> Segments { get; }

    public Transcript(string text, List<TranscriptSegment>? segments = null)
    {
        Text = text ?? string.Empty;
        Segments = segments ?? new List<TranscriptSegment>();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Text;
    }
}

public sealed class TranscriptSegment : ValueObject
{
    public string Text { get; }
    public TimeSpan StartTime { get; }
    public TimeSpan EndTime { get; }
    public decimal Confidence { get; }

    public TranscriptSegment(string text, TimeSpan startTime, TimeSpan endTime, decimal confidence)
    {
        Text = text ?? string.Empty;
        StartTime = startTime;
        EndTime = endTime;
        Confidence = confidence;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Text;
        yield return StartTime;
        yield return EndTime;
        yield return Confidence;
    }
}

