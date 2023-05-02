using Dav.AspNetCore.Server.Locks;

namespace Dav.AspNetCore.Server.Store.Files;

public class LocalFileStore : FileStore
{
    private readonly LocalFileStoreOptions options;

    /// <summary>
    /// Initializes a new <see cref="LocalFileStore"/> class.
    /// </summary>
    /// <param name="options">The local file store options.</param>
    /// <param name="lockManager">The lock manager.</param>
    public LocalFileStore(
        LocalFileStoreOptions options,
        ILockManager lockManager) 
        : base(lockManager)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        this.options = options;
    }

    public override ValueTask<bool> DirectoryExistsAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(options.RootPath, uri.LocalPath.TrimStart('/'));
        return ValueTask.FromResult(System.IO.Directory.Exists(path));
    }

    public override ValueTask<bool> FileExistsAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(options.RootPath, uri.LocalPath.TrimStart('/'));
        return ValueTask.FromResult(System.IO.File.Exists(path));
    }

    public override ValueTask DeleteDirectoryAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(options.RootPath, uri.LocalPath.TrimStart('/'));
        System.IO.Directory.Delete(path);
        
        return ValueTask.CompletedTask;
    }

    public override ValueTask DeleteFileAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(options.RootPath, uri.LocalPath.TrimStart('/'));
        System.IO.File.Delete(path);
        
        return ValueTask.CompletedTask;
    }

    public override ValueTask<DirectoryProperties> GetDirectoryPropertiesAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(options.RootPath, uri.LocalPath.TrimStart('/'));
        var directoryInfo = new DirectoryInfo(path);
        var directoryProperties = new DirectoryProperties(
            uri,
            directoryInfo.Name,
            directoryInfo.CreationTimeUtc,
            directoryInfo.LastWriteTimeUtc);

        return ValueTask.FromResult(directoryProperties);
    }

    public override ValueTask<FileProperties> GetFilePropertiesAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(options.RootPath, uri.LocalPath.TrimStart('/'));
        var fileInfo = new FileInfo(path);
        var fileProperties = new FileProperties(
            uri,
            fileInfo.Name,
            fileInfo.CreationTimeUtc,
            fileInfo.LastWriteTimeUtc,
            fileInfo.Length);

        return ValueTask.FromResult(fileProperties);
    }

    public override ValueTask<Stream> OpenFileStreamAsync(Uri uri, OpenFileMode mode, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(options.RootPath, uri.LocalPath.TrimStart('/'));
        return ValueTask.FromResult<Stream>(mode == OpenFileMode.Read 
            ? System.IO.File.OpenRead(path) 
            : System.IO.File.OpenWrite(path));
    }

    public override ValueTask CreateDirectoryAsync(Uri uri, CancellationToken cancellationToken)
    {
        var path = Path.Combine(options.RootPath, uri.LocalPath.TrimStart('/'));
        System.IO.Directory.CreateDirectory(path);
        
        return ValueTask.CompletedTask;
    }

    public override ValueTask<Uri[]> GetFilesAsync(Uri uri, CancellationToken cancellationToken)
    {
        var path = Path.Combine(options.RootPath, uri.LocalPath.TrimStart('/'));
        return ValueTask.FromResult(System.IO.Directory.GetFiles(path).Select(x =>
        {
            var relativePath = $"/{Path.GetRelativePath(options.RootPath, x)}";
            return new Uri(relativePath);
        }).ToArray());
    }

    public override ValueTask<Uri[]> GetDirectoriesAsync(Uri uri, CancellationToken cancellationToken)
    {
        var path = Path.Combine(options.RootPath, uri.LocalPath.TrimStart('/'));
        return ValueTask.FromResult(System.IO.Directory.GetDirectories(path).Select(x =>
        {
            var relativePath = $"/{Path.GetRelativePath(options.RootPath, x)}";
            return new Uri(relativePath);
        }).ToArray());
    }
}