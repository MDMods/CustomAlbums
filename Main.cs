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

        public const string MelonName = "CustomAlbums";
        public const string MelonAuthor = "Two Fellas";
        public const string MelonVersion = "4.1.5";

        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();

            if (!Directory.Exists(AlbumManager.SearchPath)) Directory.CreateDirectory(AlbumManager.SearchPath);
            
            ModSettings.Register();
            AssetPatch.AttachHook();
            SavePatch.AttachHook();
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

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);
            MusicStageCellPatch.CurrentScene = sceneName;
        }
    }
}