using LibraryManagement.Exceptions;
using LibraryManagement.Models;
using LibraryManagement.Storage;

namespace LibraryManagement.Services;

public sealed class LibraryService
{
    public const int LoanPeriodDays = 14;
    public const int MaxLoansPerMember = 5;

    private readonly ILibraryStore store;
    private readonly TimeProvider clock;
    private readonly LibraryData data;

    public LibraryService(ILibraryStore store, TimeProvider? clock = null)
    {
        this.store = store;
        this.clock = clock ?? TimeProvider.System;
        data = store.Load();
    }

    public IReadOnlyList<Book> Books => data.Books;
    public IReadOnlyList<Member> Members => data.Members;

    public Book AddBook(string title, string author, string isbn = "")
    {
        title = Require(title, "Title");
        author = Require(author, "Author");
        isbn = (isbn ?? "").Trim();

        if (isbn.Length > 0 && data.Books.Any(b => b.Isbn.Equals(isbn, StringComparison.OrdinalIgnoreCase)))
            throw new LibraryException($"A book with ISBN {isbn} already exists.");

        var book = new Book
        {
            Id = NextId(data.Books.Select(b => b.Id)),
            Title = title,
            Author = author,
            Isbn = isbn
        };
        data.Books.Add(book);
        Persist(() => data.Books.Remove(book));
        return book;
    }

    public Member RegisterMember(string name, string email)
    {
        name = Require(name, "Name");
        email = Require(email, "Email");

        if (!email.Contains('@') || email.StartsWith('@') || email.EndsWith('@'))
            throw new LibraryException("Email must look like name@example.com.");
        if (data.Members.Any(m => m.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            throw new LibraryException($"A member with email {email} is already registered.");

        var member = new Member { Id = NextId(data.Members.Select(m => m.Id)), Name = name, Email = email };
        data.Members.Add(member);
        Persist(() => data.Members.Remove(member));
        return member;
    }

    public Book GetBook(int id) =>
        data.Books.FirstOrDefault(b => b.Id == id)
        ?? throw new LibraryException($"No book with ID {id} exists.");

    public Member GetMember(int id) =>
        data.Members.FirstOrDefault(m => m.Id == id)
        ?? throw new LibraryException($"No member with ID {id} exists.");

    public IReadOnlyList<Book> Search(string query)
    {
        query = Require(query, "Search term");
        return data.Books
            .Where(b => Contains(b.Title, query) || Contains(b.Author, query) || Contains(b.Isbn, query))
            .ToList();
    }

    public Book Borrow(int bookId, int memberId)
    {
        var book = GetBook(bookId);
        var member = GetMember(memberId);

        if (book.Status == BookStatus.Borrowed)
            throw new LibraryException($"'{book.Title}' is already borrowed.");

        int activeLoans = data.Books.Count(b => b.BorrowedByMemberId == member.Id);
        if (activeLoans >= MaxLoansPerMember)
            throw new LibraryException($"{member.Name} already has {MaxLoansPerMember} books on loan.");

        book.Status = BookStatus.Borrowed;
        book.BorrowedByMemberId = member.Id;
        book.DueOn = Today().AddDays(LoanPeriodDays);

        Persist(() =>
        {
            book.Status = BookStatus.Available;
            book.BorrowedByMemberId = null;
            book.DueOn = null;
        });
        return book;
    }

    public ReturnReceipt Return(int bookId)
    {
        var book = GetBook(bookId);
        if (book.Status != BookStatus.Borrowed)
            throw new LibraryException($"'{book.Title}' is not currently borrowed.");

        var previousMember = book.BorrowedByMemberId;
        var previousDue = book.DueOn;
        int daysOverdue = previousDue is null ? 0 : Math.Max(0, Today().DayNumber - previousDue.Value.DayNumber);

        book.Status = BookStatus.Available;
        book.BorrowedByMemberId = null;
        book.DueOn = null;

        Persist(() =>
        {
            book.Status = BookStatus.Borrowed;
            book.BorrowedByMemberId = previousMember;
            book.DueOn = previousDue;
        });
        return new ReturnReceipt(book, daysOverdue);
    }

    /// <summary>Saves to disk. If saving fails, the in-memory change is undone so memory and file never disagree.</summary>
    private void Persist(Action rollback)
    {
        try
        {
            store.Save(data);
        }
        catch
        {
            rollback();
            throw;
        }
    }

    private DateOnly Today() => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

    private static int NextId(IEnumerable<int> existingIds) => existingIds.DefaultIfEmpty(0).Max() + 1;

    private static bool Contains(string text, string query) =>
        text.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static string Require(string? value, string field)
    {
        var trimmed = value?.Trim() ?? "";
        if (trimmed.Length == 0)
            throw new LibraryException($"{field} cannot be empty.");
        return trimmed;
    }
}
