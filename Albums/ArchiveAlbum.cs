using System.IO;
using System.IO.Compression;
using CustomAlbums.Data;
using CustomAlbums.Utilities;

namespace CustomAlbums.Albums;

public class ArchiveAlbum : Album
{
    public override string AlbumName => $"album_{System.IO.Path.GetFileNameWithoutExtension(Path)}";

    public ArchiveAlbum(string path, int index)
    {
        if (!File.Exists(path))
        {
            Logger.Error($"Could not find album package at: {path}");
            throw new FileNotFoundException();
        }

        using var zip = ZipFile.OpenRead(path);
        var info = zip.GetEntry("info.json");
        if (info == null)
        {
            Logger.Error($"Could not find info.json in package: {path}");
            throw new FileNotFoundException();
        }

        using var stream = info.Open();
        Info = Json.Deserialize<AlbumInfo>(stream);
        HasPng = zip.GetEntry("cover.png") != null;

        Index = index;
        Path = path;

        GetSheets();
    }

    public override bool HasFile(string name)
    {
        if (!File.Exists(Path)) return false;
        try
        {
            using var zip = ZipFile.OpenRead(Path);
            return zip.GetEntry(name) != null;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public override Stream OpenFileStreamIfPossible(string file)
    {
        using var zip = ZipFile.OpenRead(Path);
        var entry = zip.GetEntry(file);

        if (entry != null) return entry.Open().ToMemoryStream();

        Logger.Error($"Could not find file in package: {file}");
        throw new FileNotFoundException();
    }

    public override Stream OpenNullableStream(string file)
    {
        using var zip = ZipFile.OpenRead(Path);
        var entry = zip.GetEntry(file);

        if (entry != null) return entry.Open().ToMemoryStream();

        return null;
    }

    public override MemoryStream OpenMemoryStream(string file)
    {
        using var zip = ZipFile.OpenRead(Path);
        var entry = zip.GetEntry(file);

        if (entry != null) return entry.Open().ToMemoryStream();

        Logger.Error($"Could not find file in package: {file}");
        throw new FileNotFoundException();
    }
}
