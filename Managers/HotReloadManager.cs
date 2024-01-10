using CustomAlbums.Patches;
using Il2CppAssets.Scripts.UI.Panels;
using HarmonyLib;
using Logger = CustomAlbums.Utilities.Logger;
using Il2CppAssets.Scripts.Database;

namespace CustomAlbums.Managers
{
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

            // Get the music info from the UID, remove it from the ShowMusic list, and refresh the UI
            var musicInfo = GlobalDataBase.s_DbMusicTag.GetMusicInfoFromAll($"{AlbumManager.Uid}-{album.Index}");
            GlobalDataBase.s_DbMusicTag.RemoveShowMusicUid(musicInfo);
            PnlStageInstance.m_MusicRootAnimator?.Play(PnlStageInstance.animNameAlbumIn);
            PnlStageInstance.RefreshMusicFSV();

            // TODO: Remove the album information from the Custom Albums tag menu
            
            // TODO: Only change the selected album if the selected album was the album that was deleted
            Logger.Msg("Successfully removed from cache!");
        }

        private static void RenameAllCachedAssets(string oldAlbumName, string newAlbumName)
        {
            Logger.Msg($"Renaming {oldAlbumName} to {newAlbumName}!");
            var success = AlbumManager.LoadedAlbums.Remove(oldAlbumName, out var album);
            if (!success) return;

            // rename the assets to the new name
            AlbumManager.LoadedAlbums.TryAdd(newAlbumName, album); 
            AssetPatch.ModifyCacheKey($"{oldAlbumName}_demo", $"{newAlbumName}_demo");
            AssetPatch.ModifyCacheKey($"{oldAlbumName}_music", $"{newAlbumName}_music");
            AssetPatch.ModifyCacheKey($"{oldAlbumName}_cover", $"{newAlbumName}_cover");

            Logger.Msg("Successfully modified cache!");
        }

        private static void AddNewAlbums(int previousSize)
        {
            var index = previousSize;
            for (var i = previousSize; i < AlbumManager.LoadedAlbums.Count; i++)
            {
                // TODO: Write added album hot reloading logic here
            }
            PnlStageInstance.m_MusicRootAnimator?.Play(PnlStageInstance.animNameAlbumIn);
            PnlStageInstance.RefreshMusicFSV();
        }

        /// <summary>
        /// Runs the logic for hot reloading on Unity's FixedUpdate
        /// </summary>
        internal static void FixedUpdate() 
        {
            var previousSize = AlbumManager.LoadedAlbums.Count;
            while (AlbumsAdded.Count > 0)
            {
                AlbumManager.LoadOne(AlbumsAdded.Dequeue());
            }
            while (AlbumsDeleted.Count > 0)
            {
                RemoveAllCachedAssets(AlbumsDeleted.Dequeue());
            }
        }

        /// <summary>
        /// Initializes the AlbumWatcher for adding/deleting/renaming new custom charts.
        /// </summary>
        internal static void OnLateInitializeMelon()
        {
            AlbumManager.AlbumWatcher.Path = AlbumManager.SearchPath;
            AlbumManager.AlbumWatcher.Filter = AlbumManager.SearchPattern;

            AlbumManager.AlbumWatcher.Created += (_, e) =>
            {
                Logger.Msg("Added file " + e.Name);
                while (!IsFileUnlocked(e.FullPath)) 
                {
                    // Thread sleep added to not poll the drive a ton
                    Thread.Sleep(200);
                }
                AlbumsAdded.Enqueue(e.FullPath);
            };
            AlbumManager.AlbumWatcher.Deleted += (s, e) =>
            {
                Logger.Msg("Deleted file " + e.Name);
                AlbumsDeleted.Enqueue(Path.GetFileNameWithoutExtension(e.Name));
            };
            AlbumManager.AlbumWatcher.Renamed += (s, e) =>
            {
                Logger.Msg("Renamed file " + e.OldName + " -> " + e.Name);
                RenameAllCachedAssets($"album_{Path.GetFileNameWithoutExtension(e.OldName)}", $"album_{Path.GetFileNameWithoutExtension(e.Name)}");
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
