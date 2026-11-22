namespace LibraryManagement.Models;

/// <summary>Everything the application stores. One object maps to one JSON file.</summary>
public sealed class LibraryData
{
    public int Version { get; set; } = 1;
    public List<Book> Books { get; set; } = [];
    public List<Member> Members { get; set; } = [];
}
