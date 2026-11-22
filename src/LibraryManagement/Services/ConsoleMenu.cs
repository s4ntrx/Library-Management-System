using LibraryManagement.Exceptions;
using LibraryManagement.Models;

namespace LibraryManagement.Services;

/// <summary>Text menu. Reads from and writes to injected streams so it can be driven by tests or pipes.</summary>
public sealed class ConsoleMenu(LibraryService library, TextReader input, TextWriter output)
{
    public void Run()
    {
        try
        {
            while (true)
            {
                PrintMenu();
                var choice = Ask("Choose an option").Trim();
                if (choice == "0")
                    break;

                try
                {
                    Execute(choice);
                }
                catch (LibraryException ex)
                {
                    output.WriteLine($"Error: {ex.Message}");
                }
                catch (StorageException ex)
                {
                    output.WriteLine($"Storage error: {ex.Message}");
                }
            }
        }
        catch (EndOfStreamException)
        {
            // Input closed (Ctrl+D or end of a pipe). Every change is already saved.
        }

        output.WriteLine("Goodbye.");
    }

    private void Execute(string choice)
    {
        switch (choice)
        {
            case "1":
                var book = library.AddBook(Ask("Title"), Ask("Author"), Ask("ISBN (optional)"));
                output.WriteLine($"Book saved successfully with ID {book.Id}.");
                break;
            case "2":
                PrintBooks(library.Books);
                break;
            case "3":
                var member = library.RegisterMember(Ask("Name"), Ask("Email"));
                output.WriteLine($"Member registered with ID {member.Id}.");
                break;
            case "4":
                PrintMembers();
                break;
            case "5":
                var borrowed = library.Borrow(AskInt("Book ID"), AskInt("Member ID"));
                output.WriteLine($"'{borrowed.Title}' is now borrowed. Due {borrowed.DueOn:yyyy-MM-dd}.");
                break;
            case "6":
                var receipt = library.Return(AskInt("Book ID"));
                output.WriteLine(receipt.DaysOverdue > 0
                    ? $"'{receipt.Book.Title}' returned, {receipt.DaysOverdue} day(s) overdue."
                    : $"'{receipt.Book.Title}' returned. Status: {receipt.Book.Status}.");
                break;
            case "7":
                PrintBooks([library.GetBook(AskInt("Book ID"))]);
                break;
            case "8":
                PrintBooks(library.Search(Ask("Search title, author or ISBN")));
                break;
            default:
                output.WriteLine("Unknown option. Enter a number from the menu.");
                break;
        }
    }

    private void PrintMenu()
    {
        output.WriteLine();
        output.WriteLine("1) Add book        5) Borrow book");
        output.WriteLine("2) List books      6) Return book");
        output.WriteLine("3) Add member      7) Find book by ID");
        output.WriteLine("4) List members    8) Search books");
        output.WriteLine("0) Exit");
    }

    private void PrintBooks(IEnumerable<Book> books)
    {
        var list = books.ToList();
        if (list.Count == 0)
        {
            output.WriteLine("No books found.");
            return;
        }

        output.WriteLine($"{"ID",-4} {"Title",-32} {"Author",-22} {"Status",-10} {"Due",-10}");
        foreach (var b in list)
        {
            var due = b.DueOn?.ToString("yyyy-MM-dd") ?? "-";
            output.WriteLine($"{b.Id,-4} {Clip(b.Title, 32),-32} {Clip(b.Author, 22),-22} {b.Status,-10} {due,-10}");
        }
    }

    private void PrintMembers()
    {
        if (library.Members.Count == 0)
        {
            output.WriteLine("No members registered.");
            return;
        }

        output.WriteLine($"{"ID",-4} {"Name",-24} {"Email",-30}");
        foreach (var m in library.Members)
            output.WriteLine($"{m.Id,-4} {Clip(m.Name, 24),-24} {Clip(m.Email, 30),-30}");
    }

    private string Ask(string label)
    {
        output.Write($"{label}: ");
        return input.ReadLine() ?? throw new EndOfStreamException();
    }

    private int AskInt(string label) =>
        int.TryParse(Ask(label).Trim(), out var value)
            ? value
            : throw new LibraryException("Please enter a whole number.");

    private static string Clip(string text, int width) =>
        text.Length <= width ? text : text[..(width - 1)] + "~";
}
