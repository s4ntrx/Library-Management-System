using LibraryManagement.Models;

namespace LibraryManagement.Storage;

/// <summary>
/// Where library data lives. The service only knows this interface, so the JSON file
/// can later be replaced by a database without touching the business rules.
/// </summary>
public interface ILibraryStore
{
    LibraryData Load();
    void Save(LibraryData data);
}
