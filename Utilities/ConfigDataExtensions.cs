using System.Text.Json.Nodes;
using Il2CppGameLogic;
using Il2CppPeroPeroGames.GlobalDefines;
using MelonLoader;

namespace CustomAlbums.Utilities
{
    public static class ConfigDataExtensions
    {
        public static bool IsAprilFools(this NoteConfigData config) =>
            config.prefab_name.EndsWith("_fool");

        public static NoteType GetNoteType(this NoteConfigData config) =>
            (NoteType)config.type;

        public static bool IsAnyScene(this NoteConfigData config) =>
            config.scene == "0";

        public static bool IsAnyPathway(this NoteConfigData config) =>
            config.pathway == 0 && config.score == 0 && config.fever == 0 && config.damage == 0;

        public static bool IsAnySpeed(this NoteConfigData config) =>
            config.GetNoteType() == NoteType.Boss
            || config.GetNoteType() == NoteType.None
            || config.ibms_id == "16"
            || config.ibms_id == "17";

        public static MusicConfigData ToMusicConfigData(this JsonNode node)
        {
            MelonLogger.Msg(node.ToJsonString());
            // Converting decimal -> Il2CppSystem.Decimal is ass, so decimal -> float -> Il2CppSystem.Decimal
            var time = node["time"]?.GetValue<decimal>() ?? decimal.Zero;
            var length = node["length"]?.GetValue<decimal>() ?? decimal.Zero;
            
            var config = Interop.CreateTypeValue<MusicConfigData>();
            config.id = node["id"]?.GetValue<int>() ?? -1;
            config.time = (Il2CppSystem.Decimal)(float)time;
            config.note_uid = node["note_uid"]?.GetValue<string>() ?? string.Empty;
            config.length = (Il2CppSystem.Decimal)(float)length;
            config.pathway = node["pathway"]?.GetValue<int>() ?? 0;
            config.blood = node["blood"]?.GetValue<bool>() ?? false;

            return config;
        }
    }
}
