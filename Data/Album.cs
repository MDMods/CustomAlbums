using System.IO.Compression;
using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using UnityEngine;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums.Data
{
    public class Album
    {
        private readonly Logger _logger = new(nameof(Album));

        public Album(string path, int index)
        {
            if (Directory.Exists(path))
            {
                // Load album from directory
                if (!File.Exists($"{path}\\info.json"))
                {
                    _logger.Error($"Could not find info.json at: {path}\\info.json");
                    throw new FileNotFoundException();
                }

                using var fileStream = File.OpenRead($"{path}\\info.json");
                Info = Json.Deserialize<AlbumInfo>(fileStream);
            }
            else if (File.Exists(path))
            {
                // Load album from package
                using var zip = ZipFile.OpenRead(path);
                var info = zip.GetEntry("info.json");
                if (info == null)
                {
                    _logger.Error($"Could not find info.json in package: {path}");
                    throw new FileNotFoundException();
                }

                using var stream = info.Open();
                Info = Json.Deserialize<AlbumInfo>(stream);
                IsPackaged = true;
            }
            else
            {
                _logger.Error($"Could not find album at: {path}");
                throw new FileNotFoundException();
            }

            Index = index;
            Path = path;

            GetSheets();
        }

        public int Index { get; }
        public string Path { get; }
        public bool IsPackaged { get; }
        public AlbumInfo Info { get; }
        public Sprite Cover => this.GetCover();
        public AnimatedCover AnimatedCover => this.GetAnimatedCover();
        public AudioClip Music => this.GetAudio();
        public AudioClip Demo => this.GetAudio("demo");
        public Dictionary<int, Sheet> Sheets { get; } = new();
        public string AlbumName =>
            IsPackaged ? $"album_{System.IO.Path.GetFileNameWithoutExtension(Path)}" : $"album_{System.IO.Path.GetFileName(Path)}_folder";

        public bool HasFile(string name)
        {
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
            if (IsPackaged)
            {
                using var zip = ZipFile.OpenRead(Path);
                var entry = zip.GetEntry(file);

                if (entry != null)
                {
                    return entry.Open().ToMemoryStream();
                }

                _logger.Error($"Could not find file in package: {file}");
                throw new FileNotFoundException();
            }

            var path = $"{Path}\\{file}";
            if (File.Exists(path))
                return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            _logger.Error($"Could not find file: {path}");
            throw new FileNotFoundException();
        }

        public MemoryStream OpenMemoryStream(string file)
        {
            if (IsPackaged)
            {
                using var zip = ZipFile.OpenRead(Path);
                var entry = zip.GetEntry(file);

                if (entry != null)
                {
                    return entry.Open().ToMemoryStream();
                }

                _logger.Error($"Could not find file in package: {file}");
                throw new FileNotFoundException();
            }

            var path = $"{Path}\\{file}";
            if (File.Exists(path))
                return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite).ToMemoryStream();

            _logger.Error($"Could not find file: {path}");
            throw new FileNotFoundException();
        }
        private void GetSheets()
        {
            // Adds to the Sheets dictionary
            foreach (var difficulty in Info.Difficulties.Keys.Where(difficulty => HasFile($"map{difficulty}.bms")))
            {
                using var stream = OpenMemoryStream($"map{difficulty}.bms");
                var hash = stream.GetHash();

                Sheets.Add(difficulty, new Sheet(hash, this, difficulty));
            }
        }
    }
}