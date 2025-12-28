using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Tests.Repositories;

public abstract class RepositoryTestBase : IDisposable
{
    protected ApplicationDbContext Context { get; }
    protected IClock Clock { get; }

    protected RepositoryTestBase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new ApplicationDbContext(options);
        Clock = new TestClock();
    }

    protected async Task SaveChangesAsync()
    {
        await Context.SaveChangesAsync();
    }

    public void Dispose()
    {
        Context.Dispose();
    }

    private class TestClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}










