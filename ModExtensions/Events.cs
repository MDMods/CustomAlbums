using CustomAlbums.Managers;
using CustomAlbums.Patches;

namespace CustomAlbums.ModExtensions;

public static class Events
{
    public delegate void LoadAlbumEvent(object s, AlbumEventArgs e);

    public delegate void LoadAssetEvent(object s, AssetEventArgs e);

    public static event LoadAssetEvent OnAssetLoaded
    {
        add => AssetPatch.OnAssetLoaded += value;
        remove => AssetPatch.OnAssetLoaded -= value;
    }

    public static event LoadAlbumEvent OnAlbumLoaded
    {
        add => AlbumManager.OnAlbumLoaded += value;
        remove => AlbumManager.OnAlbumLoaded -= value;
    }
}