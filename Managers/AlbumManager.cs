using CustomAlbums.Data;
using CustomAlbums.ModExtensions;
using CustomAlbums.Utilities;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.GeneralLocalization;
using Il2CppPeroTools2.Resources;
using System.IO.Compression;
using UnityEngine;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums.Managers
{
    public static class AlbumManager
    {
        public const int Uid = 999;
        public const string SearchPath = "Custom_Albums";
        public const string SearchPattern = "*.mdm";
        public const string PackSearchPattern = "*.mdp";
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
        internal static Events.LoadAlbumEvent OnAlbumLoaded;

        private static int MaxCount { get; set; }
        internal static string CurrentPack { get; set; } = null;
        public static Dictionary<string, Album> LoadedAlbums { get; } = new();


        public static void LoadPack(string directory)
        {
            // Get the files from the directory
            try
            {
                var zipFiles = ZipFile.OpenRead(directory);

                // Filter for .mdm files and find the pack.json file
                var mdms = zipFiles.Entries.Where(file => file.Name.EndsWith(".mdm"));
                var json = zipFiles.Entries.FirstOrDefault(file => file.Name.EndsWith(".json"));

                // Initialize pack and variables
                var pack = PackManager.CreatePack(json);
                CurrentPack = pack.Title;
                pack.StartIndex = MaxCount;

                // Count successfully loaded .mdm files
                pack.Length = mdms.Count(mdm => LoadOne(directory, mdm, mdm.Name) != null);

                // Set the current pack to null and add the pack to the pack list
                CurrentPack = null;
                PackManager.AddPack(pack);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to load album at {directory}. Reason: {ex.Message}");
                Logger.Warning(ex.StackTrace);
            }
        }

        public static Album LoadOne(string directory, ZipArchiveEntry mdm, string fullFileName)
        {
            var fileName = Path.GetFileNameWithoutExtension(fullFileName);
            MaxCount = Math.Max(LoadedAlbums.Count, MaxCount);
            
            if (LoadedAlbums.ContainsKey(fileName)) return null;

            try
            {
                var album = new Album(directory, mdm, MaxCount, CurrentPack);
                if (album.Info is null) return null;

                var albumName = album.AlbumName;
                Logger.Msg("Adding " + albumName + " as a pack!");

                LoadedAlbums.Add(albumName, album);

                if (album.HasFile("cover.png") || album.HasFile("cover.gif"))
                    ResourcesManager.instance.LoadFromName<Sprite>($"{albumName}_cover").hideFlags |=
                        HideFlags.DontUnloadUnusedAsset;

                Logger.Msg($"Loaded {albumName}: {album.Info.Name}");
                OnAlbumLoaded?.Invoke(typeof(AlbumManager), new AlbumEventArgs(album));
                return album;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to load album at {fileName}. Reason: {ex.Message}");
                Logger.Warning(ex.StackTrace);
            }

            return null;
        }

        public static Album LoadOne(string path)
        {
            MaxCount = Math.Max(LoadedAlbums.Count, MaxCount);
            var isDirectory = File.GetAttributes(path).HasFlag(FileAttributes.Directory);
            var fileName = isDirectory ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path);
            
            if (LoadedAlbums.ContainsKey(fileName)) return null;
            
            try
            {
                var album = new Album(path, MaxCount, CurrentPack);
                if (album.Info is null) return null;

                var albumName = album.AlbumName;
                
                LoadedAlbums.Add(albumName, album);

                if (album.HasFile("cover.png") || album.HasFile("cover.gif"))
                    ResourcesManager.instance.LoadFromName<Sprite>($"{albumName}_cover").hideFlags |=
                        HideFlags.DontUnloadUnusedAsset;

                Logger.Msg($"Loaded {albumName}: {album.Info.Name}");
                OnAlbumLoaded?.Invoke(typeof(AlbumManager), new AlbumEventArgs(album));
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
            
            var packs = new List<string>();
            var files = new List<string>();
            files.AddRange(Directory.GetFiles(SearchPath, SearchPattern));
            files.AddRange(Directory.GetDirectories(SearchPath));
            packs.AddRange(Directory.GetFiles(SearchPath, PackSearchPattern));

            foreach (var file in files) LoadOne(file);
            foreach (var pack in packs) LoadPack(pack);

            Logger.Msg($"Finished loading {LoadedAlbums.Count} albums.", false);
        }

        public static IEnumerable<string> GetAllUid()
        {
            return LoadedAlbums.Select(album => $"{Uid}-{album.Value.Index}");
        }

        public static Album GetByUid(string uid)
        {
            return LoadedAlbums.FirstOrDefault(album => album.Value.Index == uid[4..].ParseAsInt()).Value;
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

        /// <summary>
        ///     Gets the current "Custom Albums" title based on language.
        /// </summary>
        /// <returns>The current "Custom Albums" title based on language.</returns>
        public static string GetCustomAlbumsTitle()
        {
            return Languages.GetValueOrDefault(
                SingletonScriptableObject<LocalizationSettings>
                .instance?
                .GetActiveOption("Language") ?? "English");
        }
    }
}