using System.Text.Json;
using System.Text.Json.Serialization;
using LibraryManagement.Exceptions;
using LibraryManagement.Models;

namespace LibraryManagement.Storage;

public sealed class JsonFileLibraryStore(string path) : ILibraryStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public LibraryData Load()
    {
        if (!File.Exists(path))
            return new LibraryData();

        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<LibraryData>(stream, Options) ?? new LibraryData();
        }
        catch (JsonException ex)
        {
            throw new StorageException(
                $"The data file '{path}' is corrupt. Repair it or move it aside. Details: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new StorageException($"Cannot read '{path}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Writes to a temporary file, forces it to disk, then swaps it in. If the process is
    /// killed mid-write, the previous data file is still intact.
    /// </summary>
    public void Save(LibraryData data)
    {
        var tempPath = path + ".tmp";
        try
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, data, Options);
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new StorageException($"Cannot write '{path}': {ex.Message}", ex);
        }
    }
}
