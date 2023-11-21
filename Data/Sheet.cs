using System.Text;
using System.Text.Json.Nodes;
using CustomAlbums.Utilities;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.GameCore;
using Il2CppAssets.Scripts.Structs;
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

        private readonly Logger _logger = new(nameof(Sheet));

        public Sheet(string md5, Album parentAlbum, int difficulty)
        {
            Md5 = md5;
            ParentAlbum = parentAlbum;
            Difficulty = difficulty;
            MapName = $"album_{parentAlbum.Index}_map{difficulty}";
        }

        public StageInfo GetStage()
        {
            using var stream = ParentAlbum.OpenFileStream($"map{Difficulty}.bms");

            var bms = BmsLoader.Load(stream, MapName);
            if (bms is null) return null;
                
            var stageInfo = ScriptableObject.CreateInstance<StageInfo>();
            stageInfo.mapName = MapName;
            stageInfo.scene = (string)bms.Info["GENRE"];
            stageInfo.music = $"{ParentAlbum.Index}";
            stageInfo.difficulty = Difficulty;
            // TODO: stageInfo.bpm = bms.GetBpm();
            stageInfo.md5 = Md5;
            // TODO: stageInfo.sceneEvents = bms.GetSceneEvents();
            stageInfo.name = ParentAlbum.Info.Name;

            if (ParentAlbum.HasFile($"map{Difficulty}.talk"))
            {
                using var talkStream = ParentAlbum.OpenFileStream($"map{Difficulty}.talk");
                var data = talkStream.ReadFully();
                if (data != null)
                {
                    var talkFile = Json.Deserialize<JsonObject>(Encoding.UTF8.GetString(data));
                    if (talkFile.TryGetPropertyValue("version", out var node) && node?.GetValue<int>() == 2)
                    {
                        _logger.Msg("Version 2 talk file!");
                        ParentAlbum.TalkFileVersionsForDifficulty[Difficulty - 1] = true;
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

            return stageInfo;
        }
    }
}
