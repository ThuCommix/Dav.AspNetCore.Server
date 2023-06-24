namespace Dav.AspNetCore.Server.Store.Files;

public class LocalFileStore : FileStore
{
    private readonly LocalFileStoreOptions options;

    /// <summary>
    /// Initializes a new <see cref="LocalFileStore"/> class.
    /// </summary>
    /// <param name="options">The local file store options.</param>
    public LocalFileStore(LocalFileStoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        this.options = options;
    }

    public override ValueTask<bool> DirectoryExistsAsync(ResourcePath resourcePath, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(options.RootPath, resourcePath.ToFilePath().TrimStart(Path.DirectorySeparatorChar));
        return ValueTask.FromResult(System.IO.Directory.Exists(path));
    }

    public override ValueTask<bool> FileExistsAsync(ResourcePath resourcePath, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(options.RootPath, resourcePath.ToFilePath().TrimStart(Path.DirectorySeparatorChar));
        return ValueTask.FromResult(System.IO.File.Exists(path));
    }

    public override ValueTask DeleteDirectoryAsync(ResourcePath resourcePath, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(options.RootPath, resourcePath.ToFilePath().TrimStart(Path.DirectorySeparatorChar));
        System.IO.Directory.Delete(path);
        
        return ValueTask.CompletedTask;
    }

    public override ValueTask DeleteFileAsync(ResourcePath resourcePath, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(options.RootPath, resourcePath.ToFilePath().TrimStart(Path.DirectorySeparatorChar));
        System.IO.File.Delete(path);
        
        return ValueTask.CompletedTask;
    }

    public override ValueTask<DirectoryProperties> GetDirectoryPropertiesAsync(ResourcePath resourcePath, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(options.RootPath, resourcePath.ToFilePath().TrimStart(Path.DirectorySeparatorChar));
        var directoryInfo = new DirectoryInfo(path);
        var directoryProperties = new DirectoryProperties(
            resourcePath,
            directoryInfo.Name,
            directoryInfo.CreationTimeUtc,
            directoryInfo.LastWriteTimeUtc);

        return ValueTask.FromResult(directoryProperties);
    }

    public override ValueTask<FileProperties> GetFilePropertiesAsync(ResourcePath resourcePath, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(options.RootPath, resourcePath.ToFilePath().TrimStart(Path.DirectorySeparatorChar));
        var fileInfo = new FileInfo(path);
        var fileProperties = new FileProperties(
            resourcePath,
            fileInfo.Name,
            fileInfo.CreationTimeUtc,
            fileInfo.LastWriteTimeUtc,
            fileInfo.Length);

        return ValueTask.FromResult(fileProperties);
    }

    public override ValueTask<Stream> OpenFileStreamAsync(ResourcePath resourcePath, OpenFileMode mode, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(options.RootPath, resourcePath.ToFilePath().TrimStart(Path.DirectorySeparatorChar));
        return ValueTask.FromResult<Stream>(mode == OpenFileMode.Read 
            ? System.IO.File.OpenRead(path) 
            : System.IO.File.OpenWrite(path));
    }

    public override ValueTask CreateDirectoryAsync(ResourcePath resourcePath, CancellationToken cancellationToken)
    {
        var path = Path.Combine(options.RootPath, resourcePath.ToFilePath().TrimStart(Path.DirectorySeparatorChar));
        System.IO.Directory.CreateDirectory(path);
        
        return ValueTask.CompletedTask;
    }

    public override ValueTask<ResourcePath[]> GetFilesAsync(ResourcePath resourcePath, CancellationToken cancellationToken)
    {
        var path = Path.Combine(options.RootPath, resourcePath.ToFilePath().TrimStart(Path.DirectorySeparatorChar));
        return ValueTask.FromResult(System.IO.Directory.GetFiles(path).Select(x =>
        {
            var relativePath = $"/{Path.GetRelativePath(options.RootPath, x)}";
            return new ResourcePath(relativePath);
        }).ToArray());
    }

    public override ValueTask<ResourcePath[]> GetDirectoriesAsync(ResourcePath resourcePath, CancellationToken cancellationToken)
    {
        var path = Path.Combine(options.RootPath, resourcePath.ToFilePath().TrimStart(Path.DirectorySeparatorChar));
        return ValueTask.FromResult(System.IO.Directory.GetDirectories(path).Select(x =>
        {
            var relativePath = $"/{Path.GetRelativePath(options.RootPath, x)}";
            return new ResourcePath(relativePath);
        }).ToArray());
    }
}