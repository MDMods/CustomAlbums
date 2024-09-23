using System.Text.Json.Nodes;
using CustomAlbums.Utilities;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.GameCore;
using Il2CppAssets.Scripts.Structs;

namespace CustomAlbums.Data
{
    public class Sheet
    {
        private static readonly Logger Logger = new(nameof(Sheet));

        public Sheet(Album parentAlbum, int difficulty)
        {
            ParentAlbum = parentAlbum;
            Difficulty = difficulty;
            MapName = $"{parentAlbum.AlbumName}_map{difficulty}";
        }

        public Album ParentAlbum { get; }
        public string MapName { get; }
        public int Difficulty { get; }
        public bool TalkFileVersion2 { get; set; }
        public string Md5
        {
            get
            {
                using var stream = ParentAlbum.OpenMemoryStream($"map{Difficulty}.bms");
                return stream.GetHash();
            }
        }

        public StageInfo GetStage()
        {
            // If opening a FileStream is possible (i.e. reading from a folder) then open it as FileStream
            // Otherwise open it as a MemoryStream
            // This allows writing to the map BMS file while it is being read
            using var stream = ParentAlbum.OpenFileStreamIfPossible($"map{Difficulty}.bms");

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
                using var talkStream = ParentAlbum.OpenFileStreamIfPossible($"map{Difficulty}.talk");
                if (talkStream.Length > 0)
                {
                    var talkFile = Json.Deserialize<JsonObject>(talkStream);
                    if (talkFile.TryGetPropertyValue("version", out var node) && node?.GetValue<int>() == 2)
                    {
                        Logger.Msg("Version 2 talk file!");
                        TalkFileVersion2 = true;
                    }

                    talkFile.Remove("version");
                    var dict = Json
                        .Il2CppJsonDeserialize<
                            Il2CppSystem.Collections.Generic.Dictionary<string,
                                Il2CppSystem.Collections.Generic.List<GameDialogArgs>>>(talkFile.ToJsonString());
                    stageInfo.dialogEvents =
                        new Il2CppSystem.Collections.Generic.Dictionary<string,
                            Il2CppSystem.Collections.Generic.List<GameDialogArgs>>();
                    foreach (var dialogEvent in dict) stageInfo.dialogEvents.Add(dialogEvent.Key, dialogEvent.Value);
                }
            }

            GlobalDataBase.dbStageInfo.SetStageInfo(stageInfo);
            return stageInfo;
        }
    }
}