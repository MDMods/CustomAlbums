using System.Collections;
using System.IO.Compression;
using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using UnityEngine;
using UnityEngine.Networking;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums.Data
{
    public class Album
    {
        private static readonly Logger Logger = new(nameof(Album));

        public Album(string directory, ZipArchiveEntry mdm, int index, string packName = null)
        {
            if (!string.IsNullOrEmpty(packName)) PackName = packName;
            
            using var mdmStream = mdm.Open();
            using var openedZip = new ZipArchive(mdmStream);

            var info = openedZip.GetEntry("info.json");
            if (info == null)
            {
                Logger.Error($"Could not find info.json in package: {mdm.Name}");
                throw new FileNotFoundException();
            }

            using var stream = info.Open();
            Info = Json.Deserialize<AlbumInfo>(stream);
            IsPackaged = true;

            // CurrentPack will always be null if album is not in a pack
            IsPack = AlbumManager.CurrentPack != null;

            HasPng = openedZip.GetEntry("cover.png") != null;

            Index = index;
            Path = directory;
            PackAlbumName = System.IO.Path.GetFileNameWithoutExtension(mdm.Name);
            GetSheets();
        }

        public Album(string path, int index, string packName = null)
        {
            // If packName is not null then it's a file path, not a folder chart
            if (Directory.Exists(path) && packName == null)
            {
                // Load album from directory
                if (!File.Exists($"{path}\\info.json"))
                {
                    Logger.Error($"Could not find info.json at: {path}\\info.json");
                    throw new FileNotFoundException();
                }

                using var fileStream = File.OpenRead($"{path}\\info.json");
                Info = Json.Deserialize<AlbumInfo>(fileStream);
                HasPng = File.Exists(System.IO.Path.Combine(path, "cover.png"));
            }
            else if (File.Exists(path))
            {
                // Load album from package
                PackName = packName;
                using var zip = ZipFile.OpenRead(path);
                var info = zip.GetEntry("info.json");
                if (info == null)
                {
                    Logger.Error($"Could not find info.json in package: {path}");
                    throw new FileNotFoundException();
                }

                using var stream = info.Open();
                Info = Json.Deserialize<AlbumInfo>(stream);
                IsPackaged = true;
                
                // CurrentPack will always be null if album is not in a pack
                IsPack = AlbumManager.CurrentPack != null;

                HasPng = zip.GetEntry("cover.png") != null;
            }
            else
            {
                Logger.Error($"Could not find album at: {path}");
                throw new FileNotFoundException();
            }

            Index = index;
            Path = path;

            GetSheets();
        }

        public string PackAlbumName { get; } = string.Empty;
        public int Index { get; }
        public string Path { get; }
        public bool IsPackaged { get; }
        public bool IsPack { get; }
        public bool HasPng { get; }
        public string PackName { get; }
        public AlbumInfo Info { get; }

        private Sprite _cover;
        private bool _isCoverLoading;
        private static Sprite _defaultCover;
        public static Sprite DefaultCover 
        {
            get 
            {
                if (_defaultCover != null) return _defaultCover;
                var tex = new Texture2D(2, 2);
                _defaultCover = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
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
                    MelonLoader.MelonCoroutines.Start(LoadCoverAsync());
                }
                return DefaultCover;
            }
        }

        private IEnumerator LoadCoverAsync()
        {
            string url;
            string cacheDir = System.IO.Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "CustomAlbums", "Cache");
            if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);
            
            if (IsPack || IsPackaged)
            {
                string cacheFile = System.IO.Path.Combine(cacheDir, $"{Uid}_cover.png");
                if (System.IO.File.Exists(cacheFile)) File.Delete(cacheFile);

                using var stream = OpenNullableStream("cover.png");
                if (stream != null)
                {
                    using var fs = File.OpenWrite(cacheFile);
                    stream.CopyTo(fs);
                }
                
                url = "file:///" + cacheFile.Replace('\\', '/');
            }
            else
            {
                url = "file:///" + System.IO.Path.Combine(Path, "cover.png").Replace('\\', '/');
            }

            var request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (!request.isHttpError && !request.isNetworkError)
            {
                var texture = DownloadHandlerTexture.GetContent(request);
                texture.wrapMode = TextureWrapMode.MirrorOnce;
                _cover = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                _cover.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                
                CustomAlbums.Patches.AssetPatch.UpdateCache($"{AlbumName}_cover", _cover);
            }
            
            _isCoverLoading = false;
        }
        public AudioClip Music => this.GetAudio();
        public AudioClip Demo => this.GetAudio("demo");
        public Dictionary<int, Sheet> Sheets { get; } = new();
        public string AlbumName =>
            IsPackaged ? 
                $"album_{(string.IsNullOrEmpty(PackAlbumName) ? System.IO.Path.GetFileNameWithoutExtension(Path) : string.Empty)}{(PackName != null ? $"{PackAlbumName}_{PackName}" : string.Empty)}" 
                : $"album_{System.IO.Path.GetFileName(Path)}_folder";
        public string Uid => $"{AlbumManager.Uid}-{Index}";

        public bool HasFile(string name)
        {
            if (IsPack && !string.IsNullOrEmpty(PackAlbumName))
            {
                if (!File.Exists(Path)) return false;
                try
                {
                    using var mdp = ZipFile.OpenRead(Path);
                    using var openedMdm = mdp.GetNestedZip(PackAlbumName + ".mdm");
                    return openedMdm.GetEntry(name) != null;
                }
                catch (IOException)
                {
                    return false;
                }
            }
            if (IsPackaged)
            {
                if (!File.Exists(Path)) return false;
                try
                {
                    using var zip = ZipFile.OpenRead(Path);
                    return zip.GetEntry(name) != null;
                }
                catch (IOException)
                {
                    // This is expected in the case of deleting an album
                    return false;
                }
            }

            var path = $"{Path}\\{name}";
            return File.Exists(path);
        }

        public Stream OpenFileStreamIfPossible(string file)
        {
            if (IsPack && !string.IsNullOrEmpty(PackAlbumName))
            {
                using var mdp = ZipFile.OpenRead(Path);
                using var openedMdm = mdp.GetNestedZip(PackAlbumName + ".mdm");
                var entry = openedMdm.GetEntry(file);

                if (entry != null)
                {
                    return entry.Open().ToMemoryStream();
                }

                Logger.Error($"Could not find file in package: {file}");
                throw new FileNotFoundException();
            }
            if (IsPackaged)
            {
                using var zip = ZipFile.OpenRead(Path);
                var entry = zip.GetEntry(file);

                if (entry != null)
                {
                    return entry.Open().ToMemoryStream();
                }

                Logger.Error($"Could not find file in package: {file}");
                throw new FileNotFoundException();
            }

            var path = $"{Path}\\{file}";
            if (File.Exists(path))
                return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            Logger.Error($"Could not find file: {path}");
            throw new FileNotFoundException();
        }

        public Stream OpenNullableStream(string file)
        {
            if (IsPack && !string.IsNullOrEmpty(PackAlbumName))
            {
                using var mdp = ZipFile.OpenRead(Path);
                using var openedMdm = mdp.GetNestedZip(PackAlbumName + ".mdm");
                var entry = openedMdm.GetEntry(file);

                if (entry != null)
                {
                    return entry.Open().ToMemoryStream();
                }

                return null;
            }
            if (IsPackaged)
            {
                using var zip = ZipFile.OpenRead(Path);
                var entry = zip.GetEntry(file);

                if (entry != null)
                {
                    return entry.Open().ToMemoryStream();
                }

                return null;
            }

            var path = $"{Path}\\{file}";
            if (File.Exists(path))
                return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            return null;
        }

        public MemoryStream OpenMemoryStream(string file)
        {
            if (IsPack && !string.IsNullOrEmpty(PackAlbumName))
            {
                using var mdp = ZipFile.OpenRead(Path);
                using var openedMdm = mdp.GetNestedZip(PackAlbumName + ".mdm");
                var entry = openedMdm.GetEntry(file);

                if (entry != null)
                {
                    return entry.Open().ToMemoryStream();
                }

                Logger.Error($"Could not find file in package: {file}");
                throw new FileNotFoundException();
            }

            if (IsPackaged)
            {
                using var zip = ZipFile.OpenRead(Path);
                var entry = zip.GetEntry(file);

                if (entry != null)
                {
                    return entry.Open().ToMemoryStream();
                }

                Logger.Error($"Could not find file in package: {file}");
                throw new FileNotFoundException();
            }

            var path = $"{Path}\\{file}";
            if (File.Exists(path))
                return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite).ToMemoryStream();

            Logger.Error($"Could not find file: {path}");
            throw new FileNotFoundException();
        }
        private void GetSheets()
        {
            // Adds to the Sheets dictionary
            foreach (var difficulty in Info.Difficulties.Keys.Where(difficulty => HasFile($"map{difficulty}.bms")))
                Sheets.Add(difficulty, new Sheet(this, difficulty));
        }

        public bool HasDifficulty(int difficulty) => Sheets.ContainsKey(difficulty);
    }
}