using LibraryManagement.Exceptions;
using LibraryManagement.Services;
using LibraryManagement.Storage;
using Xunit;

namespace LibraryManagement.Tests;

public sealed class JsonFileLibraryStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private string DataFile => Path.Combine(directory, "library.json");

    public void Dispose()
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public void Restart_application_loads_previously_saved_books()
    {
        var firstRun = new LibraryService(new JsonFileLibraryStore(DataFile));
        firstRun.AddBook("Clean Code", "Robert C. Martin");
        var member = firstRun.RegisterMember("Amina", "amina@example.com");
        firstRun.Borrow(1, member.Id);

        var secondRun = new LibraryService(new JsonFileLibraryStore(DataFile));

        Assert.Single(secondRun.Books);
        Assert.Equal("Clean Code", secondRun.Books[0].Title);
        Assert.Equal(Models.BookStatus.Borrowed, secondRun.Books[0].Status);
        Assert.Single(secondRun.Members);
    }

    [Fact]
    public void A_missing_file_starts_an_empty_library()
    {
        var data = new JsonFileLibraryStore(DataFile).Load();

        Assert.Empty(data.Books);
        Assert.Empty(data.Members);
    }

    [Fact]
    public void A_corrupt_file_raises_a_storage_error()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(DataFile, "{ this is not json");

        Assert.Throws<StorageException>(() => new JsonFileLibraryStore(DataFile).Load());
    }

    [Fact]
    public void Saving_leaves_no_temporary_file_behind()
    {
        new JsonFileLibraryStore(DataFile).Save(new Models.LibraryData());

        Assert.True(File.Exists(DataFile));
        Assert.False(File.Exists(DataFile + ".tmp"));
    }
}
