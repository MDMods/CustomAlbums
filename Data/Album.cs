using System.Collections;
using System.Reflection;
using CustomAlbums.Managers;
using CustomAlbums.Patches;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using UnityEngine.Networking;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums.Data;

public abstract class Album
{
    protected static readonly Logger Logger = new(nameof(Album));
    private static Sprite _defaultCover;

    private Sprite _cover;
    private bool _isCoverLoading;

    public int Index { get; internal set; }
    public string Path { get; protected set; }
    public bool HasPng { get; protected set; }
    public AlbumInfo Info { get; protected set; }

    public abstract string AlbumName { get; }
    public abstract bool IsPackaged { get; }

    public Dictionary<int, Sheet> Sheets { get; } = new();

    public static Sprite DefaultCover
    {
        get
        {
            if (_defaultCover != null) return _defaultCover;

            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("CustomAlbums.Resources.default_cover.png");

            Texture2D tex;
            if (stream != null)
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                tex.LoadImage(ms.ToArray());
            }
            else
            {
                tex = new Texture2D(2, 2);
            }

            _defaultCover = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            _defaultCover.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return _defaultCover;
        }
    }

    public Sprite Cover
    {
        get
        {
            if (_cover != null) return _cover;
            if (!_isCoverLoading && HasPng)
            {
                _isCoverLoading = true;
                MelonCoroutines.Start(LoadCoverAsync());
            }

            return DefaultCover;
        }
    }

    public bool HasCover
    {
        get
        {
            if (HasPng)
            {
                HotReloadManager.PnlStageInstance?.RefreshStageUI();
                return true;
            }

            return false;
        }
    }

    public AudioClip Music => this.GetAudio();
    public AudioClip Demo => this.GetAudio("demo");
    public string Uid => $"{AlbumManager.Uid}-{Index}";

    protected void GetSheets()
    {
        foreach (var difficulty in Info.Difficulties.Keys.Where(difficulty => HasFile($"map{difficulty}.bms")))
            Sheets.Add(difficulty, new Sheet(this, difficulty));
    }

    public bool HasDifficulty(int difficulty)
    {
        return Sheets.ContainsKey(difficulty);
    }

    private IEnumerator LoadCoverAsync()
    {
        var cacheDir = System.IO.Path.Combine(MelonEnvironment.UserDataDirectory, "CustomAlbums", "Cache");
        if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

        var url = GetCoverUrl(cacheDir);

        var request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (!request.isHttpError && !request.isNetworkError)
        {
            var texture = DownloadHandlerTexture.GetContent(request);
            texture.wrapMode = TextureWrapMode.MirrorOnce;
            _cover = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            _cover.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            AssetPatch.UpdateCache($"{AlbumName}_cover", _cover);
            CoverRefreshPatch.Refresh($"{AlbumManager.Uid}-{Index}");
        }

        _isCoverLoading = false;
    }

    protected virtual string GetCoverUrl(string cacheDir)
    {
        var cacheFile = System.IO.Path.Combine(cacheDir, $"{Uid}_cover.png");
        if (File.Exists(cacheFile)) File.Delete(cacheFile);

        using var stream = OpenNullableStream("cover.png");
        if (stream != null)
        {
            using var fs = File.OpenWrite(cacheFile);
            stream.CopyTo(fs);
        }

        return "file:///" + cacheFile.Replace('\\', '/');
    }

    public abstract bool HasFile(string name);
    public abstract Stream OpenFileStreamIfPossible(string file);
    public abstract Stream OpenNullableStream(string file);
    public abstract MemoryStream OpenMemoryStream(string file);
}