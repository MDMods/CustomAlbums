using System.IO;
using System.IO.Compression;
using CustomAlbums.Data;
using CustomAlbums.Utilities;

namespace CustomAlbums.Albums;

public class PackAlbum : Album
{
    public string PackName { get; }
    public string PackAlbumName { get; }

    public override string AlbumName => $"album_{(string.IsNullOrEmpty(PackAlbumName) ? System.IO.Path.GetFileNameWithoutExtension(Path) : string.Empty)}{(PackName != null ? $"{PackAlbumName}_{PackName}" : string.Empty)}";

    public PackAlbum(string directory, ZipArchiveEntry mdm, int index, string packName = null)
    {
        if (!string.IsNullOrEmpty(packName)) PackName = packName;

        using var mdmStream = mdm.Open();
        using var openedZip = new ZipArchive(mdmStream);

        var info = openedZip.GetEntry("info.json");
        if (info == null)
        {
            Logger.Error($"Could not find info.json in package: {mdm.Name}");
            throw new FileNotFoundException();
        }

        using var stream = info.Open();
        Info = Json.Deserialize<AlbumInfo>(stream);
        HasPng = openedZip.GetEntry("cover.png") != null;

        Index = index;
        Path = directory;
        PackAlbumName = System.IO.Path.GetFileNameWithoutExtension(mdm.Name);
        
        GetSheets();
    }

    public override bool HasFile(string name)
    {
        if (!File.Exists(Path)) return false;
        try
        {
            using var mdp = ZipFile.OpenRead(Path);
            using var openedMdm = mdp.GetNestedZip(PackAlbumName + ".mdm");
            return openedMdm.GetEntry(name) != null;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public override Stream OpenFileStreamIfPossible(string file)
    {
        using var mdp = ZipFile.OpenRead(Path);
        using var openedMdm = mdp.GetNestedZip(PackAlbumName + ".mdm");
        var entry = openedMdm.GetEntry(file);

        if (entry != null) return entry.Open().ToMemoryStream();

        Logger.Error($"Could not find file in package: {file}");
        throw new FileNotFoundException();
    }

    public override Stream OpenNullableStream(string file)
    {
        using var mdp = ZipFile.OpenRead(Path);
        using var openedMdm = mdp.GetNestedZip(PackAlbumName + ".mdm");
        var entry = openedMdm.GetEntry(file);

        if (entry != null) return entry.Open().ToMemoryStream();

        return null;
    }

    public override MemoryStream OpenMemoryStream(string file)
    {
        using var mdp = ZipFile.OpenRead(Path);
        using var openedMdm = mdp.GetNestedZip(PackAlbumName + ".mdm");
        var entry = openedMdm.GetEntry(file);

        if (entry != null) return entry.Open().ToMemoryStream();

        Logger.Error($"Could not find file in package: {file}");
        throw new FileNotFoundException();
    }
}
