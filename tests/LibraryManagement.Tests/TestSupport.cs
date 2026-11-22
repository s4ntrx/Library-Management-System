using LibraryManagement.Exceptions;
using LibraryManagement.Models;
using LibraryManagement.Storage;

namespace LibraryManagement.Tests;

/// <summary>Keeps data in memory so service rules can be tested without touching the disk.</summary>
internal sealed class InMemoryStore : ILibraryStore
{
    private readonly LibraryData data = new();

    public bool FailOnSave { get; set; }

    public LibraryData Load() => data;

    public void Save(LibraryData _)
    {
        if (FailOnSave)
            throw new StorageException("Simulated disk failure.");
    }
}

/// <summary>A clock the test controls, so due dates and overdue days are predictable.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset now = start;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => now;

    public void Advance(TimeSpan amount) => now += amount;
}
