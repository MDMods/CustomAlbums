using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.GameCore;
using Il2CppAssets.Scripts.Structs;
using UnityEngine;

namespace CustomAlbums.Data
{
    public class Album
    {
        public int Index { get; }
        public string Path { get; }
        public bool IsPackaged { get; }
        public bool[] TalkFileVersionsForDifficulty = new bool[4];
        public AlbumInfo Info { get; }
        public Sprite Cover => this.GetCover();
        public AnimatedCover AnimatedCover => this.GetAnimatedCover();
        public AudioClip Music => this.GetAudio();
        public AudioClip Demo => this.GetAudio("demo");
        public Dictionary<int, Sheet> Sheets { get; } = new();

        private readonly Utilities.Logger _logger = new(nameof(Album));

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

                Info = Json.Deserialize<AlbumInfo>(File.ReadAllText($"{path}\\info.json"));
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

                Info = Json.Deserialize<AlbumInfo>(new StreamReader(info.Open()).ReadToEnd());
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

        public bool HasFile(string name)
        {
            if (IsPackaged)
            {
                using var zip = ZipFile.OpenRead(Path);
                return zip.GetEntry(name) != null;
            }

            var path = $"{Path}\\{name}";
            return File.Exists(path);
        }

        public MemoryStream OpenFileStream(string file)
        {
            if (IsPackaged)
            {
                using var zip = ZipFile.OpenRead(Path);
                var entry = zip.GetEntry(file);

                if (entry != null)
                    return entry.Open().ToMemoryStream();

                _logger.Error($"Could not find file in package: {file}");
                throw new FileNotFoundException();
            }

            var path = $"{Path}\\{file}";
            if (File.Exists(path))
                return File.OpenRead(path).ToMemoryStream();

            _logger.Error($"Could not find file: {path}");
            throw new FileNotFoundException();
        }

        private void GetSheets()
        {
            // Adds to the Sheets dictionary
            foreach (var difficulty in Info.Difficulties.Keys)
            {
                using var stream = OpenFileStream($"map{difficulty}.bms");
                var hash = stream.GetHash();
                var mapName = $"album_{Index}_map{difficulty}";

                var bms = BmsLoader.Load(stream, mapName);
                if (bms is null) continue;
                
                var stageInfo = ScriptableObject.CreateInstance<StageInfo>();
                stageInfo.mapName = mapName;
                stageInfo.scene = (string)bms.Info["GENRE"];
                stageInfo.music = $"{Index}";
                stageInfo.difficulty = difficulty;
                // TODO: stageInfo.bpm = bms.GetBpm();
                stageInfo.md5 = hash;
                // TODO: stageInfo.sceneEvents = bms.GetSceneEvents();
                stageInfo.name = Info.Name;

                if (HasFile($"map{difficulty}.talk"))
                {
                    using var talkStream = OpenFileStream($"map{difficulty}.talk");
                    var data = talkStream.ReadFully();
                    if (data != null)
                    {
                        var talkFile = Json.Deserialize<JsonObject>(Encoding.UTF8.GetString(data));
                        if (talkFile.TryGetPropertyValue("version", out var node) && node.GetValue<int>() == 2)
                        {
                            _logger.Msg("Version 2 talk file!");
                            TalkFileVersionsForDifficulty[difficulty - 1] = true;
                        }
                        talkFile.Remove("version");
                        var dict = Json
                            .Il2CppJsonDeserialize<
                                Il2CppSystem.Collections.Generic.Dictionary<string,
                                    Il2CppSystem.Collections.Generic.List<GameDialogArgs>>>(talkFile.ToJsonString());
                        stageInfo.dialogEvents = new Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Collections.Generic.List<GameDialogArgs>>();
                        foreach (var dialogEvent in dict)
                        {
                            stageInfo.dialogEvents.Add(dialogEvent.Key, dialogEvent.Value);
                        }
                    }
                }

                stageInfo = BmsLoader.TransmuteData(bms);
                GlobalDataBase.dbStageInfo.SetStageInfo(stageInfo);

                Sheets.Add(difficulty, new Sheet(hash, stageInfo));
            }
        }
    }
}
