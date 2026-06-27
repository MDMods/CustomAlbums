using System.IO.Compression;
using CustomAlbums.Data;
using CustomAlbums.Utilities;

namespace CustomAlbums.Managers;

public class PackManager
{
    private static readonly List<Pack> Packs = new();

    public static Pack GetPackFromUid(string uid)
    {
        // If the uid is not custom or parsing the index fails
        if (!uid.StartsWith($"{AlbumManager.Uid}-") ||
            !uid[4..].TryParseAsInt(out var uidIndex)) return null;

        // Retrieve the pack that the uid belongs to
        var retrievedPack = Packs.FirstOrDefault(pack =>
            uidIndex >= pack.StartIndex && uidIndex < pack.StartIndex + pack.Length);

        // If the pack has no albums in it return null, otherwise return pack (will be null if it doesn't exist)
        return retrievedPack?.Length is 0 ? null : retrievedPack;
    }

    internal static Pack CreatePack(ZipArchiveEntry json, string path)
    {
        var pack = Json.Deserialize<Pack>(json.Open());
        pack.Path = path;
        return pack;
    }

    internal static void AddPack(Pack pack)
    {
        Packs.Add(pack);
    }
}