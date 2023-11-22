using CustomAlbums.Managers;
using CustomAlbums.Patches;
using MelonLoader;
using static CustomAlbums.Patches.AnimatedCoverPatch;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums
{
    public class Main : MelonMod
    {
        private static readonly Logger Logger = new(nameof(Main));
        private static readonly Stack<string> AlbumAdded = new();
        private static readonly Stack<string> AlbumDeleted = new();
        private static bool IsFileUnlocked(string path)
        {
            try
            {
                Logger.Msg($"Waiting for {path}...");
                using var fileStream = File.Open(path, FileMode.Open);
                if (fileStream.Length <= 0) return false;

                Logger.Msg("File is ready to be read now");
                return true;
            }
            catch (Exception ex) when (ex is FileNotFoundException or IOException)
            {
                return false;
            }
        }

        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            AssetPatch.AttachHook();
            ModSettings.Register();
            AlbumManager.LoadAlbums();
        }

        public override void OnLateInitializeMelon()
        {
            base.OnLateInitializeMelon();
            AlbumManager.AlbumWatcher.Path = AlbumManager.SEARCH_PATH;
            AlbumManager.AlbumWatcher.Filter = AlbumManager.SEARCH_PATTERN;

            AlbumManager.AlbumWatcher.Created += (s, e) =>
            {
                Logger.Msg("Added file " + e.Name);
                AlbumAdded.Push(e.FullPath);
            };
            AlbumManager.AlbumWatcher.Deleted += (s, e) =>
            {
                Logger.Msg("Deleted file " + e.Name);
                AlbumDeleted.Push(e.FullPath);
            };

            AlbumManager.AlbumWatcher.EnableRaisingEvents = true;
        }

        public override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            AlbumManager.AlbumWatcher.Dispose();
        }

        /// <summary>
        /// This override adds support for animated covers.
        /// </summary>
        public override void OnUpdate()
        {
            MusicStageCellPatch.AnimateCovers();
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            while (AlbumAdded.Count > 0)
            {
                var albumPath = AlbumAdded.Pop();
                while (!IsFileUnlocked(albumPath)) { }
                AlbumManager.LoadOne(albumPath);
            }
            while (AlbumDeleted.Count > 0)
            {
                AlbumManager.LoadedAlbums.Remove(AlbumDeleted.Pop());
            }
        }
    }
}