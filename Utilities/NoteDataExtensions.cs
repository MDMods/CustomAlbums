using Il2CppGameLogic;
using Il2CppPeroPeroGames.GlobalDefines;

namespace CustomAlbums.Utilities
{
    public static class NoteConfigDataExtensions
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
    }
}
