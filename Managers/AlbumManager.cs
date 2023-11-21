using CustomAlbums.Data;
using Il2CppPeroTools2.Resources;
using UnityEngine;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums.Managers
{
    public static class AlbumManager
    {
        public const int UID = 999;
        public const string SEARCH_PATH = "Custom_Albums";
        public const string SEARCH_PATTERN = "*.mdm";
        public static readonly string JsonName = $"ALBUM{UID + 1}";
        public static readonly string MusicPackage = $"music_package_{UID}";
        public static readonly Dictionary<string, string> Languages = new()
        {
            { "English", "Custom Albums" },
            { "ChineseS", "自定义" },
            { "ChineseT", "自定義" },
            { "Japanese", "カスタムアルバム" },
            { "Korean", "커스텀앨범" }
        };

        public static Dictionary<string, Album> LoadedAlbums { get; } = new();
        private static readonly Logger Logger = new(nameof(AlbumManager));
        internal static readonly FileSystemWatcher AlbumWatcher = new();
        public static void LoadOne(string path)
        {
            var index = LoadedAlbums.Count;
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (LoadedAlbums.ContainsKey(fileName)) return;

            try
            {
                var album = new Album(path, index);
                if (album.Info is null) return;

                LoadedAlbums.Add($"album_{fileName}", album);

                if (album.HasFile("cover.png") || album.HasFile("cover.gif"))
                    ResourcesManager.instance.LoadFromName<Sprite>($"album_{fileName}_cover").hideFlags |= HideFlags.DontUnloadUnusedAsset;

                Logger.Msg($"Loaded album_{album.Index}: {album.Info.Name}");
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to load album at {fileName}. Reason: {ex.Message}");
                Logger.Warning(ex.StackTrace);
            }
        }

        public static void LoadAlbums()
        {
            LoadedAlbums.Clear();

            var files = new List<string>();
            files.AddRange(Directory.GetFiles(SEARCH_PATH, SEARCH_PATTERN));
            files.AddRange(Directory.GetDirectories(SEARCH_PATH));

            foreach (var file in files)
            {
                LoadOne(file);
            }

            Logger.Msg($"Finished loading {LoadedAlbums.Count} albums.", false);
        }

        public static IEnumerable<string> GetAllUid() =>
            LoadedAlbums.Select(album => $"{UID}-{album.Value.Index}");

        public static Album GetByUid(string uid) =>
            LoadedAlbums.FirstOrDefault(album => album.Value.Index == int.Parse(uid[4..])).Value;
    }
}
