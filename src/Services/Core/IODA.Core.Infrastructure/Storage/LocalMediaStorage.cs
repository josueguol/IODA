using IODA.Core.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IODA.Core.Infrastructure.Storage;

/// <summary>
/// Almacena archivos en el sistema de archivos local. Ruta base configurable (Media:StoragePath o Media:RootPath).
/// Estructura: {root}/{projectId}/{guid}_{fileName}
/// </summary>
public class LocalMediaStorage : IMediaStorage
{
    private readonly string _rootPath;
    private readonly ILogger<LocalMediaStorage> _logger;

    public LocalMediaStorage(IConfiguration configuration, ILogger<LocalMediaStorage> logger)
    {
        _rootPath = configuration["Media:StoragePath"] ?? configuration["Media:RootPath"] ?? Path.Combine(Path.GetTempPath(), "ioda-media");
        _logger = logger;
        if (!Directory.Exists(_rootPath))
            Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, string contentType, Guid projectId, CancellationToken cancellationToken = default)
    {
        var safeName = SanitizeFileName(fileName);
        var uniqueName = $"{Guid.NewGuid():N}_{safeName}";
        var projectDir = Path.Combine(_rootPath, projectId.ToString("N"));
        if (!Directory.Exists(projectDir))
            Directory.CreateDirectory(projectDir);
        var fullPath = Path.Combine(projectDir, uniqueName);
        var storageKey = $"{projectId:N}/{uniqueName}";

        await using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
        {
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        _logger.LogDebug("Saved media to {Path}, key {Key}", fullPath, storageKey);
        return storageKey;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storageKey);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Media file not found.", storageKey);
        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream>(stream);
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storageKey);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storageKey);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    private string GetFullPath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.IndexOf("..", StringComparison.Ordinal) >= 0)
            throw new ArgumentException("Invalid storage key.", nameof(storageKey));
        return Path.Combine(_rootPath, storageKey.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var name = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrEmpty(name) ? "file" : name;
    }
}
