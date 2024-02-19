using System.Globalization;
using System.IO.Enumeration;
using CustomAlbums.Data;
using Il2CppPeroTools2.Resources;
using UnityEngine;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums.Managers
{
    public static class AlbumManager
    {
        public const int Uid = 999;
        public const string SearchPath = "Custom_Albums";
        public const string SearchPattern = "*.mdm";
        public static readonly string JsonName = $"ALBUM{Uid + 1}";
        public static readonly string MusicPackage = $"music_package_{Uid}";

        public static readonly Dictionary<string, string> Languages = new()
        {
            { "English", "Custom Albums" },
            { "ChineseS", "自定义" },
            { "ChineseT", "自定義" },
            { "Japanese", "カスタムアルバム" },
            { "Korean", "커스텀앨범" }
        };

        private static readonly Logger Logger = new(nameof(AlbumManager));
        internal static readonly FileSystemWatcher AlbumWatcher = new();

        private static int MaxCount { get; set; }
        public static Dictionary<string, Album> LoadedAlbums { get; } = new();

        public static Album LoadOne(string path)
        {
            MaxCount = Math.Max(LoadedAlbums.Count, MaxCount);
            var fileName = File.GetAttributes(path).HasFlag(FileAttributes.Directory) ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path);
            if (LoadedAlbums.ContainsKey(fileName)) return null;

            try
            {
                var album = new Album(path, MaxCount);
                if (album.Info is null) return null;

                var albumName = album.AlbumName;
                LoadedAlbums.Add(albumName, album);

                if (album.HasFile("cover.png") || album.HasFile("cover.gif"))
                    ResourcesManager.instance.LoadFromName<Sprite>($"{albumName}_cover").hideFlags |=
                        HideFlags.DontUnloadUnusedAsset;

                Logger.Msg($"Loaded {albumName}: {album.Info.Name}");
                return album;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to load album at {fileName}. Reason: {ex.Message}");
                Logger.Warning(ex.StackTrace);
            }

            return null;
        }

        public static void LoadAlbums()
        {
            LoadedAlbums.Clear();

            var files = new List<string>();
            files.AddRange(Directory.GetFiles(SearchPath, SearchPattern));
            files.AddRange(Directory.GetDirectories(SearchPath));

            foreach (var file in files) LoadOne(file);

            Logger.Msg($"Finished loading {LoadedAlbums.Count} albums.", false);
        }

        public static IEnumerable<string> GetAllUid()
        {
            return LoadedAlbums.Select(album => $"{Uid}-{album.Value.Index}");
        }

        public static Album GetByUid(string uid)
        {
            return LoadedAlbums.FirstOrDefault(album => album.Value.Index == int.Parse(uid[4..], CultureInfo.InvariantCulture)).Value;
        }
        public static string GetAlbumNameFromUid(string uid)
        {
            var album = GetByUid(uid);
            return album is null ? string.Empty : album.AlbumName;
        }
        public static IEnumerable<string> GetAlbumUidsFromNames(this IEnumerable<string> albumNames)
        {
            return albumNames.Where(name => LoadedAlbums.ContainsKey(name))
                .Select(name => $"{Uid}-{LoadedAlbums[name].Index}");
        }
    }
}