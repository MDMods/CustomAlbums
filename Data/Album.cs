using System.IO.Compression;
using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using UnityEngine;
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
        public string PackName { get; }
        public AlbumInfo Info { get; }
        public Sprite Cover => this.GetCover();
        public AnimatedCover AnimatedCover => this.GetAnimatedCover();
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