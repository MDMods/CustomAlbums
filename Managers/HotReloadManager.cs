using CustomAlbums.Data;
using CustomAlbums.Patches;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.Managers;
using Il2CppAssets.Scripts.UI.Panels;

namespace CustomAlbums.Managers
{
    // TODO: Fix all of this with album.AlbumName
    internal static class HotReloadManager
    {
        private static readonly Logger Logger = new(nameof(HotReloadManager));
        private static Queue<string> AlbumsAdded { get; } = new();
        private static Queue<string> AlbumsDeleted { get; } = new();
        private static List<string> AlbumUidsAdded { get; } = new();
        private static PnlStage PnlStageInstance { get; set; }

        private static bool IsFileUnlocked(string path)
        {
            try
            {
                using var fileStream = File.Open(path, FileMode.Open);
                if (fileStream.Length <= 0) return false;

                Logger.Msg("The added album is ready to be read!");
                return true;
            }
            catch (Exception ex) when (ex is FileNotFoundException or IOException)
            {
                return false;
            }
        }

        private static void RemoveAllCachedAssets(string albumName)
        {
            Logger.Msg("Removing " + albumName + "!");

            if (!AlbumManager.LoadedAlbums.TryGetValue($"album_{albumName}", out var album)) return;

            // Remove cached album information, not needed anymore since the album has been deleted
            AlbumManager.LoadedAlbums.Remove($"album_{albumName}");
            CoverManager.CachedAnimatedCovers.Remove(album.Index);
            CoverManager.CachedCovers.Remove(album.Index);
            AssetPatch.RemoveFromCache($"album_{albumName}_demo");
            AssetPatch.RemoveFromCache($"album_{albumName}_music");
            AssetPatch.RemoveFromCache($"album_{albumName}_cover");

            // Get the music info from the UID and remove it from ALBUM1000 (custom albums)
            var musicInfo = GlobalDataBase.s_DbMusicTag.GetMusicInfoFromAll($"{AlbumManager.Uid}-{album.Index}");
            var customAlbumsTag = GlobalDataBase.dbMusicTag.GetAlbumTagInfo(AlbumManager.Uid);
            customAlbumsTag.musicUids.Remove($"{AlbumManager.Uid}-{album.Index}");
            var configAlbum = Singleton<ConfigManager>.instance.GetConfigObject<DBConfigALBUM>(AlbumManager.Uid + 1);
            configAlbum.m_Items.Remove(musicInfo);

            //Remove the MusicInfo from the ShowMusic list, and refresh the UI
            GlobalDataBase.s_DbMusicTag.RemoveShowMusicUid(musicInfo);
            PnlStageInstance.m_MusicRootAnimator?.Play(PnlStageInstance.animNameAlbumIn);
            PnlStageInstance.RefreshMusicFSV();

            Logger.Msg("Successfully removed from cache!");
        }

        private static void AddNewAlbum(int previousSize, string path)
        {
            //var album = AlbumManager.LoadOne(path);
            //GlobalDataBase.s_DbMusicTag.m_StageShowMusicUids.Add("");
        }

        private static void AddNewAlbums(int previousSize)
        {
            for (; previousSize < AlbumManager.LoadedAlbums.Count; previousSize++)
            {
                // TODO: Write added album hot reloading logic here
            }

            PnlStageInstance.m_MusicRootAnimator?.Play(PnlStageInstance.animNameAlbumIn);
            PnlStageInstance.RefreshMusicFSV();
        }

        /// <summary>
        ///     Runs the logic for hot reloading on Unity's FixedUpdate
        /// </summary>
        internal static void FixedUpdate()
        {
            var previousSize = AlbumManager.LoadedAlbums.Count;
            while (AlbumsAdded.Count > 0)
            {
                var albumName = AlbumsAdded.Dequeue();
                AddNewAlbum(previousSize, albumName);
                Console.WriteLine($"Added album via hot-reload: \"{albumName}\"");
            }
            while (AlbumsDeleted.Count > 0)
            {
                var albumName = AlbumsDeleted.Dequeue();
                RemoveAllCachedAssets(albumName);
                Console.WriteLine($"Removed album via hot-reload: \"{albumName}\"");
            }
        }

        /// <summary>
        ///     Initializes the AlbumWatcher for adding/deleting/renaming new custom charts.
        /// </summary>
        internal static void OnLateInitializeMelon()
        {
            AlbumManager.AlbumWatcher.Path = AlbumManager.SearchPath;
            AlbumManager.AlbumWatcher.Filter = AlbumManager.SearchPattern;

            AlbumManager.AlbumWatcher.Created += (_, e) =>
            {
                Logger.Msg("Added file " + e.Name);
                while (!IsFileUnlocked(e.FullPath))
                    // Thread sleep added to not poll the drive a ton
                    Thread.Sleep(200);
                AlbumsAdded.Enqueue(e.FullPath);
            };
            AlbumManager.AlbumWatcher.Deleted += (s, e) =>
            {
                Logger.Msg("Deleted file " + e.Name);
                AlbumsDeleted.Enqueue(Path.GetFileNameWithoutExtension(e.Name));
            };

            // Start the AlbumWatcher
            AlbumManager.AlbumWatcher.EnableRaisingEvents = true;
        }

        [HarmonyPatch(typeof(PnlStage), nameof(PnlStage.PreWarm))]
        internal static class StagePreWarmPatch
        {
            private static void Postfix(PnlStage __instance)
            {
                Logger.Msg($"PnlStage instance retrieved. Null = {__instance == null}");
                PnlStageInstance = __instance;
            }
        }
    }
}