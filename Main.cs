using CustomAlbums.Managers;
using CustomAlbums.Patches;
using CustomAlbums.Utilities;
using MelonLoader;
using static CustomAlbums.Patches.AnimatedCoverPatch;

namespace CustomAlbums
{
    public class Main : MelonMod
    {
        private static readonly Logger Logger = new("CustomAlbums");

        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();

            if (!Directory.Exists(AlbumManager.SearchPath)) Directory.CreateDirectory(AlbumManager.SearchPath);

            AssetPatch.AttachHook();
            SavePatch.AttachHook();
            ModSettings.Register();
            AlbumManager.LoadAlbums();
            SaveManager.LoadSaveFile();
            Logger.Msg("Initialized CustomAlbums!", false);
        }

        public override void OnLateInitializeMelon()
        {
            base.OnLateInitializeMelon();
            // TODO: Actually write HotReload
            // HotReloadManager.OnLateInitializeMelon();
        }

        public override void OnApplicationQuit()
        {
            if (ModSettings.SavingEnabled) SaveManager.SaveSaveFile();
            base.OnApplicationQuit();
        }

        /// <summary>
        ///     This override adds support for animated covers.
        /// </summary>
        public override void OnUpdate()
        {
            base.OnUpdate();
            MusicStageCellPatch.AnimateCoversUpdate();
        }

        /// <summary>
        ///     This override adds support for hot reloading.
        /// </summary>
        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            // TODO: Actually write HotReload
            // HotReloadManager.FixedUpdate();
        }
    }
}