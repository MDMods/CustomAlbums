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

        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            AssetPatch.AttachHook();
            ModSettings.Register();
            AlbumManager.LoadAlbums();
            Logger.Msg("Initialized CustomAlbums!");
        }

        public override void OnLateInitializeMelon()
        {
            base.OnLateInitializeMelon();
            HotReloadManager.OnLateInitializeMelon();
        }

        /// <summary>
        /// This override adds support for animated covers.
        /// </summary>
        public override void OnUpdate()
        {
            base.OnUpdate();
            MusicStageCellPatch.AnimateCoversUpdate();
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            HotReloadManager.FixedUpdate();
        }
    }
}