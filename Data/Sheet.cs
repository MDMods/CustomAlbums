using System.Text;
using System.Text.Json.Nodes;
using CustomAlbums.Utilities;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.GameCore;
using Il2CppAssets.Scripts.Structs;
using Il2CppGameLogic;
using UnityEngine;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums.Data
{
    public class Sheet
    {
        public Album ParentAlbum { get; }
        public string Md5 { get; }
        public string MapName { get; }
        public int Difficulty { get; }
        public bool TalkFileVersion2 { get; set; } = false;

        private readonly Logger _logger = new(nameof(Sheet));

        public Sheet(string md5, Album parentAlbum, int difficulty)
        {
            Md5 = md5;
            ParentAlbum = parentAlbum;
            Difficulty = difficulty;
            MapName = $"album_{Path.GetFileNameWithoutExtension(parentAlbum.Path)}_map{difficulty}";
        }

        public StageInfo GetStage()
        {
            using var stream = ParentAlbum.OpenFileStream($"map{Difficulty}.bms");

            var bms = BmsLoader.Load(stream, MapName);
            if (bms is null) return null;

            var stageInfo = BmsLoader.TransmuteData(bms);
            stageInfo.mapName = MapName;
            stageInfo.scene = bms.Info["GENRE"]?.GetValue<string>() ?? string.Empty;
            stageInfo.music = $"{ParentAlbum.Index}";
            stageInfo.difficulty = Difficulty;
            stageInfo.bpm = bms.Bpm;
            stageInfo.md5 = Md5;
            stageInfo.sceneEvents = bms.GetSceneEvents();
            stageInfo.name = ParentAlbum.Info.Name;

            if (ParentAlbum.HasFile($"map{Difficulty}.talk"))
            {
                using var talkStream = ParentAlbum.OpenFileStream($"map{Difficulty}.talk");
                if (talkStream.Length > 0)
                {
                    var talkFile = Json.Deserialize<JsonObject>(talkStream);
                    if (talkFile.TryGetPropertyValue("version", out var node) && node?.GetValue<int>() == 2)
                    {
                        _logger.Msg("Version 2 talk file!");
                        TalkFileVersion2 = true;
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

            GlobalDataBase.dbStageInfo.SetStageInfo(stageInfo);
            return stageInfo;
        }
    }
}
