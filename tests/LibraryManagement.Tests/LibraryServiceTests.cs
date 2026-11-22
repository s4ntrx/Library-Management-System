using LibraryManagement.Exceptions;
using LibraryManagement.Models;
using LibraryManagement.Services;
using Xunit;

namespace LibraryManagement.Tests;

public sealed class LibraryServiceTests
{
    private static readonly DateTimeOffset Start = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryStore store = new();
    private readonly FixedTimeProvider clock = new(Start);
    private readonly LibraryService library;

    public LibraryServiceTests() => library = new LibraryService(store, clock);

    [Fact]
    public void Add_new_book_saves_it_with_an_id()
    {
        var book = library.AddBook("Clean Code", "Robert C. Martin", "9780132350884");

        Assert.Equal(1, book.Id);
        Assert.Equal(BookStatus.Available, book.Status);
        Assert.Single(library.Books);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Add_book_rejects_a_blank_title(string title)
    {
        Assert.Throws<LibraryException>(() => library.AddBook(title, "Someone"));
    }

    [Fact]
    public void Add_book_rejects_a_duplicate_isbn()
    {
        library.AddBook("Clean Code", "Robert C. Martin", "123");

        Assert.Throws<LibraryException>(() => library.AddBook("Another", "Author", "123"));
    }

    [Fact]
    public void Display_books_returns_every_stored_book()
    {
        library.AddBook("One", "A");
        library.AddBook("Two", "B");

        Assert.Equal(2, library.Books.Count);
    }

    [Fact]
    public void Borrow_available_book_changes_status_to_borrowed()
    {
        var book = library.AddBook("Clean Code", "Robert C. Martin");
        var member = library.RegisterMember("Amina", "amina@example.com");

        library.Borrow(book.Id, member.Id);

        Assert.Equal(BookStatus.Borrowed, book.Status);
        Assert.Equal(member.Id, book.BorrowedByMemberId);
        Assert.Equal(new DateOnly(2026, 3, 15), book.DueOn);
    }

    [Fact]
    public void Borrow_a_book_that_is_already_borrowed_fails()
    {
        var book = library.AddBook("Clean Code", "Robert C. Martin");
        var first = library.RegisterMember("Amina", "amina@example.com");
        var second = library.RegisterMember("Brian", "brian@example.com");
        library.Borrow(book.Id, first.Id);

        Assert.Throws<LibraryException>(() => library.Borrow(book.Id, second.Id));
    }

    [Fact]
    public void Borrow_with_an_unknown_member_fails()
    {
        var book = library.AddBook("Clean Code", "Robert C. Martin");

        Assert.Throws<LibraryException>(() => library.Borrow(book.Id, 42));
        Assert.Equal(BookStatus.Available, book.Status);
    }

    [Fact]
    public void Borrow_beyond_the_loan_limit_fails()
    {
        var member = library.RegisterMember("Amina", "amina@example.com");
        for (int i = 1; i <= LibraryService.MaxLoansPerMember; i++)
            library.Borrow(library.AddBook($"Book {i}", "Author").Id, member.Id);
        var extra = library.AddBook("One too many", "Author");

        Assert.Throws<LibraryException>(() => library.Borrow(extra.Id, member.Id));
    }

    [Fact]
    public void Return_borrowed_book_changes_status_to_available()
    {
        var book = library.AddBook("Clean Code", "Robert C. Martin");
        var member = library.RegisterMember("Amina", "amina@example.com");
        library.Borrow(book.Id, member.Id);

        var receipt = library.Return(book.Id);

        Assert.Equal(BookStatus.Available, book.Status);
        Assert.Equal(0, receipt.DaysOverdue);
    }

    [Fact]
    public void Return_after_the_due_date_reports_days_overdue()
    {
        var book = library.AddBook("Clean Code", "Robert C. Martin");
        var member = library.RegisterMember("Amina", "amina@example.com");
        library.Borrow(book.Id, member.Id);

        clock.Advance(TimeSpan.FromDays(LibraryService.LoanPeriodDays + 6));

        Assert.Equal(6, library.Return(book.Id).DaysOverdue);
    }

    [Fact]
    public void Return_a_book_that_is_not_borrowed_fails()
    {
        var book = library.AddBook("Clean Code", "Robert C. Martin");

        Assert.Throws<LibraryException>(() => library.Return(book.Id));
    }

    [Fact]
    public void Search_for_an_invalid_id_reports_an_error()
    {
        var error = Assert.Throws<LibraryException>(() => library.GetBook(999));

        Assert.True(error.Message.Contains("999"));
    }

    [Fact]
    public void Search_matches_title_author_and_isbn_ignoring_case()
    {
        library.AddBook("Clean Code", "Robert C. Martin", "9780132350884");
        library.AddBook("The Pragmatic Programmer", "Hunt and Thomas");

        Assert.Single(library.Search("PRAGMATIC"));
        Assert.Single(library.Search("martin"));
        Assert.Single(library.Search("0132350884"));
        Assert.Empty(library.Search("cooking"));
    }

    [Fact]
    public void A_failed_save_rolls_the_change_back()
    {
        var book = library.AddBook("Clean Code", "Robert C. Martin");
        var member = library.RegisterMember("Amina", "amina@example.com");
        store.FailOnSave = true;

        Assert.Throws<StorageException>(() => library.Borrow(book.Id, member.Id));

        Assert.Equal(BookStatus.Available, book.Status);
        Assert.Null(book.BorrowedByMemberId);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    public void Register_member_rejects_an_invalid_email(string email)
    {
        Assert.Throws<LibraryException>(() => library.RegisterMember("Amina", email));
    }

    [Fact]
    public void Register_member_rejects_a_duplicate_email()
    {
        library.RegisterMember("Amina", "amina@example.com");

        Assert.Throws<LibraryException>(() => library.RegisterMember("Other", "AMINA@example.com"));
    }
}
