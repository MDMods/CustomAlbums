using CustomAlbums.Managers;
using MelonLoader;

namespace CustomAlbums
{
    public class Main : MelonMod
    {
        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            Patches.AssetPatch.AttachHook();
            ModSettings.Register();
            AlbumManager.LoadAlbums();
        }
    }
}