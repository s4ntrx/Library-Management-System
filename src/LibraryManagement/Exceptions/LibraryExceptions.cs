namespace LibraryManagement.Exceptions;

/// <summary>A rule was broken or input was invalid. The message is safe to show to the user.</summary>
public class LibraryException(string message) : Exception(message);

/// <summary>The data file could not be read or written.</summary>
public sealed class StorageException(string message, Exception? inner = null) : Exception(message, inner);
