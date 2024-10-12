using CustomAlbums.Data;
using CustomAlbums.Utilities;

namespace CustomAlbums.Managers
{
    internal class PackManager
    {
        private static readonly List<Pack> Packs = new();
        internal static Pack GetPackFromUid(string uid)
        {
            // If the uid is not custom or parsing the index fails
            if (!uid.StartsWith($"{AlbumManager.Uid}-") || 
                !uid[4..].TryParseAsInt(out var uidIndex)) return null;

            // Retrieve the pack that the uid belongs to
            var pack = Packs.FirstOrDefault(pack =>
                uidIndex >= pack.StartIndex && uidIndex < pack.StartIndex + pack.Length);

            // If the pack has no albums in it return null, otherwise return pack (will be null if it doesn't exist)
            return pack?.Length == 0 ? null : pack;
        }

        internal static Pack CreatePack(string file)
        {
            return Json.Deserialize<Pack>(File.OpenRead(file));
        }

        internal static void AddPack(Pack pack)
        {
            Packs.Add(pack);
        }
    }
}
