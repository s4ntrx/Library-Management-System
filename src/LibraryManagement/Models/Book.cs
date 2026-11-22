namespace LibraryManagement.Models;

public enum BookStatus
{
    Available,
    Borrowed
}

public sealed class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Isbn { get; set; } = "";
    public BookStatus Status { get; set; } = BookStatus.Available;
    public int? BorrowedByMemberId { get; set; }
    public DateOnly? DueOn { get; set; }
}
