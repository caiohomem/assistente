using AssistenteExecutivo.Domain.Interfaces;

namespace AssistenteExecutivo.Application.Tests.Helpers;

public class TestClock : IClock
{
    public DateTime UtcNow { get; private set; }

    public TestClock(DateTime? fixedTime = null)
    {
        UtcNow = fixedTime ?? DateTime.UtcNow;
    }

    public void Advance(TimeSpan timeSpan)
    {
        UtcNow = UtcNow.Add(timeSpan);
    }

    public void SetTime(DateTime time)
    {
        UtcNow = time;
    }
}














