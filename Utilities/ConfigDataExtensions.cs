using CustomAlbums.Data;
using Il2CppGameLogic;
using Il2CppPeroPeroGames.GlobalDefines;
using Decimal = Il2CppSystem.Decimal;

namespace CustomAlbums.Utilities;

public static class ConfigDataExtensions
{
    public static bool IsAprilFools(this NoteConfigData config)
    {
        return config.prefab_name.EndsWith("_fool");
    }

    public static NoteType GetNoteType(this NoteConfigData config)
    {
        return (NoteType)config.type;
    }

    public static bool IsAnyScene(this NoteConfigData config)
    {
        return config.scene == "0";
    }

    public static bool IsAnyPathway(this NoteConfigData config)
    {
        return config.pathway == 0 && config.score == 0 && config.fever == 0 && config.damage == 0;
    }

    public static bool IsPhase2BossGear(this NoteConfigData config)
    {
        return config.GetNoteType() == NoteType.Block && config.boss_action.EndsWith("_atk_2");
    }

    public static bool IsAnySpeed(this NoteConfigData config)
    {
        return config.GetNoteType() == NoteType.Boss
               || config.GetNoteType() == NoteType.None
               || config.ibms_id == "16"
               || config.ibms_id == "17";
    }

    /// <summary>
    ///     Converts a <see cref="ProcessedNote" /> to a <see cref="MusicConfigData" /> Il2Cpp struct.
    /// </summary>
    public static MusicConfigData ToMusicConfigData(this ProcessedNote note)
    {
        var config = Interop.CreateTypeValue<MusicConfigData>();
        config.id = note.Id;
        config.time = (Decimal)(float)note.Time;
        config.note_uid = note.NoteUid;
        config.length = (Decimal)(float)note.Length;
        config.pathway = note.Pathway;
        config.blood = note.Blood;

        return config;
    }
}