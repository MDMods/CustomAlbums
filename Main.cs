using CustomAlbums.Managers;
using CustomAlbums.Patches;
using CustomAlbums.Utilities;
using MelonLoader;
using MelonLoader.Utils;

namespace CustomAlbums;

public class Main : MelonMod
{
    public const string MelonName = "CustomAlbums";
    public const string MelonAuthor = "Two Fellas";
    public const string MelonVersion = "4.1.9";
    private static readonly Logger Logger = new("CustomAlbums");

    public override void OnInitializeMelon()
    {
        base.OnInitializeMelon();

        if (!Directory.Exists(AlbumManager.SearchPath)) Directory.CreateDirectory(AlbumManager.SearchPath);

        var cacheDir = Path.Combine(MelonEnvironment.UserDataDirectory, "CustomAlbums", "Cache");
        if (Directory.Exists(cacheDir)) Directory.Delete(cacheDir, true);

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
        HotReloadManager.OnLateInitializeMelon();
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        // TODO: Actually write HotReload
        HotReloadManager.FixedUpdate();
    }

    public override void OnDeinitializeMelon()
    {
        base.OnDeinitializeMelon();
        var cacheDir = Path.Combine(MelonEnvironment.UserDataDirectory, "CustomAlbums", "Cache");
        if (Directory.Exists(cacheDir)) Directory.Delete(cacheDir, true);
    }
}