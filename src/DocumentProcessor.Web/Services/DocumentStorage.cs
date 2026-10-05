using DocumentProcessor.Web.Configuration;
using Microsoft.Extensions.Options;

namespace DocumentProcessor.Web.Services;

public interface IDocumentStorage
{
    Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
    Stream OpenRead(string storagePath);
}

/// <summary>Stores uploads on the local file system under a date-partitioned path.</summary>
public sealed class FileSystemDocumentStorage(
    IOptions<StorageOptions> options,
    TimeProvider timeProvider) : IDocumentStorage
{
    private readonly StorageOptions _options = options.Value;

    public async Task<string> SaveAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var uniqueName = $"{Path.GetFileNameWithoutExtension(fileName)}_{now:yyyyMMddHHmmssfff}{Path.GetExtension(fileName)}";
        var relativePath = Path.Combine(now.ToString("yyyy/MM/dd"), uniqueName);
        var fullPath = Path.Combine(_options.RootPath, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = File.Create(fullPath);
        await content.CopyToAsync(file, cancellationToken);

        return relativePath;
    }

    public Stream OpenRead(string storagePath) =>
        File.OpenRead(Path.Combine(_options.RootPath, storagePath));
}
