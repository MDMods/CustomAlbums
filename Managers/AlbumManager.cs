using CustomAlbums.Data;
using Il2CppPeroTools2.Resources;
using UnityEngine;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums.Managers
{
    public static class AlbumManager
    {
        public static readonly int Uid = 999;
        public static readonly string JsonName = $"ALBUM{Uid + 1}";
        public static readonly string MusicPackage = $"music_package_{Uid}";
        public static readonly string SearchPath = "Custom_Albums";
        public static readonly string SearchPattern = "*.mdm";
        public static readonly Dictionary<string, string> Languages = new()
        {
            { "English", "Custom Albums" },
            { "ChineseS", "自定义" },
            { "ChineseT", "自定義" },
            { "Japanese", "カスタムアルバム" },
            { "Korean", "커스텀앨범" }
        };

        public static Dictionary<string, Album> LoadedAlbums { get; } = new();
        public static List<string> Assets { get; } = new();

        private static readonly Logger Logger = new(nameof(AlbumManager));

        public static void LoadAlbums()
        {
            LoadedAlbums.Clear();
            var nextIndex = 0;

            var files = new List<string>();
            files.AddRange(Directory.GetFiles(SearchPath, SearchPattern));
            files.AddRange(Directory.GetDirectories(SearchPath));

            foreach (var file in files)
            {
                try
                {
                    var album = new Album(file, nextIndex);
                    if (album.Info is null) continue;

                    LoadedAlbums.Add($"album_{nextIndex}", album);
                    Logger.Msg($"Loaded album_{album.Index}: {album.Info.Name}");
                    nextIndex++;
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to load album at {file}. Reason: {ex.Message}");
                    Logger.Warning(ex.StackTrace);
                }
            }

            // Obtain available assets
            Assets.Clear();
            foreach (var (key, album) in LoadedAlbums)
            {
                // Ensure files exist within the album
                if (album.HasFile("demo.ogg") || album.HasFile("demo.mp3"))
                    Assets.Add($"{key}_demo");
                if (album.HasFile("music.ogg") || album.HasFile("music.mp3"))
                    Assets.Add($"{key}_music");
                if (album.HasFile("cover.png") || album.HasFile("cover.gif"))
                    Assets.Add($"{key}_cover");

                // Ensure sheets exist within the album
                if (album.HasFile("map1.bms"))
                    Assets.Add($"{key}_map1");
                if (album.HasFile("map2.bms"))
                    Assets.Add($"{key}_map2");
                if (album.HasFile("map3.bms"))
                    Assets.Add($"{key}_map3");
                if (album.HasFile("map4.bms"))
                    Assets.Add($"{key}_map4");

                // Load the chart cover and never unload it
                ResourcesManager.instance.LoadFromName<Sprite>($"{key}_cover").hideFlags |= HideFlags.DontUnloadUnusedAsset;
            }

            Logger.Msg($"Finished loading {LoadedAlbums.Count} albums with {Assets.Count} assets.", false);
        }

        public static IEnumerable<string> GetAllUid()
        {
            return LoadedAlbums.Select(album => $"{Uid}-{album.Value.Index}");
        }
    }
}
