using System.IO;
using CustomAlbums.Data;
using CustomAlbums.Utilities;

namespace CustomAlbums.Albums;

public class FolderAlbum : Album
{
    public override string AlbumName => $"album_{System.IO.Path.GetFileName(Path)}_folder";
    public override bool IsPackaged => false;

    public FolderAlbum(string path, int index)
    {
        if (!Directory.Exists(path))
        {
            Logger.Error($"Could not find album directory at: {path}");
            throw new DirectoryNotFoundException();
        }

        var infoPath = System.IO.Path.Combine(path, "info.json");
        if (!File.Exists(infoPath))
        {
            Logger.Error($"Could not find info.json at: {infoPath}");
            throw new FileNotFoundException();
        }

        using var fileStream = File.OpenRead(infoPath);
        Info = Json.Deserialize<AlbumInfo>(fileStream);
        HasPng = File.Exists(System.IO.Path.Combine(path, "cover.png"));

        Index = index;
        Path = path;

        GetSheets();
    }

    protected override string GetCoverUrl(string cacheDir)
    {
        return "file:///" + System.IO.Path.Combine(Path, "cover.png").Replace('\\', '/');
    }

    public override bool HasFile(string name)
    {
        var path = System.IO.Path.Combine(Path, name);
        return File.Exists(path);
    }

    public override Stream OpenFileStreamIfPossible(string file)
    {
        var path = System.IO.Path.Combine(Path, file);
        if (File.Exists(path))
            return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        Logger.Error($"Could not find file: {path}");
        throw new FileNotFoundException();
    }

    public override Stream OpenNullableStream(string file)
    {
        var path = System.IO.Path.Combine(Path, file);
        if (File.Exists(path))
            return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        return null;
    }

    public override MemoryStream OpenMemoryStream(string file)
    {
        var path = System.IO.Path.Combine(Path, file);
        if (File.Exists(path))
            return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite).ToMemoryStream();

        Logger.Error($"Could not find file: {path}");
        throw new FileNotFoundException();
    }
}
