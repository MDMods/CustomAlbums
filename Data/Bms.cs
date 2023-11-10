using System.Text.Json.Nodes;
using CustomAlbums.Utilities;
using Il2CppAssets.Scripts.GameCore.Managers;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppFormulaBase;
using Il2CppGameLogic;
using Il2CppPeroPeroGames.GlobalDefines;

namespace CustomAlbums.Data
{
    internal class Bms
    {
        public static Dictionary<string, NoteConfigData> NoteData { get; } = new();
        public JsonObject Info { get; set; }
        public JsonArray Notes { get; set; }
        public JsonArray NotesPercent { get; set; }
        public string Md5 { get; set; }

        [Flags]
        public enum ChannelType
        {
            /// <summary>
            /// Channel does not support anything.
            /// </summary>
            None = 0,
            /// <summary>
            /// Channel supports the Ground Lane.
            /// </summary>
            Ground = 1,
            /// <summary>
            /// Channel supports the Air Lane.
            /// </summary>
            Air = 2,
            /// <summary>
            /// Channel supports standard events.
            /// </summary>
            Event = 4,
            /// <summary>
            /// Channel supports scene events.
            /// </summary>
            Scene = 8,
            /// <summary>
            /// Notes in this channel become Heart Enemies if possible.
            /// </summary>
            SpBlood = 16,
            /// <summary>
            /// Notes in this channel become Tap Holds if possible.
            /// </summary>
            SpTapHolds = 32,
            /// <summary>
            /// This channel is used for BPM changes, with the value directly present.
            /// </summary>
            SpBpmDirect = 64,
            /// <summary>
            /// This channel is used for BPM changes, with the value placed in a lookup table.
            /// </summary>
            SpBpmLookup = 128,
            /// <summary>
            /// This channel is used for time signature changes.
            /// </summary>
            SpTimesig = 256,
            /// <summary>
            /// This channel is used for scroll speed changes.
            /// </summary>
            SpScroll = 512
        }

        public static readonly Dictionary<string, ChannelType> Channels = new() {
            ["01"] = ChannelType.Scene,
            ["02"] = ChannelType.SpTimesig,
            ["03"] = ChannelType.SpBpmDirect,
            ["08"] = ChannelType.SpBpmLookup,
            ["15"] = ChannelType.Event,
            ["16"] = ChannelType.Event,
            ["18"] = ChannelType.Event,
            ["11"] = ChannelType.Air | ChannelType.Event,
            ["13"] = ChannelType.Air | ChannelType.Event,
            ["31"] = ChannelType.Air | ChannelType.Event | ChannelType.SpBlood,
            ["33"] = ChannelType.Air | ChannelType.Event | ChannelType.SpBlood,
            ["51"] = ChannelType.Air | ChannelType.Event,
            ["53"] = ChannelType.Air | ChannelType.Event,
            ["D1"] = ChannelType.Air | ChannelType.Event | ChannelType.SpTapHolds,
            ["D3"] = ChannelType.Air | ChannelType.Event | ChannelType.SpTapHolds,
            ["12"] = ChannelType.Ground | ChannelType.Event,
            ["14"] = ChannelType.Ground | ChannelType.Event,
            ["32"] = ChannelType.Ground | ChannelType.Event | ChannelType.SpBlood,
            ["34"] = ChannelType.Ground | ChannelType.Event | ChannelType.SpBlood,
            ["52"] = ChannelType.Ground | ChannelType.Event,
            ["54"] = ChannelType.Ground | ChannelType.Event,
            ["D2"] = ChannelType.Ground | ChannelType.Event | ChannelType.SpTapHolds,
            ["D4"] = ChannelType.Ground | ChannelType.Event | ChannelType.SpTapHolds,
            ["SC"] = ChannelType.SpScroll
        };

        public static void InitNoteData()
        {
            foreach (var config in NoteDataMananger.instance.noteDatas)
            {
                if (config.IsAprilFools()) continue;
                if (config.GetNoteType() == NoteType.Block && config.boss_action.EndsWith("_atk_2")) continue;

                var speeds = new List<int> { config.speed };
                var scenes = new List<string> { config.scene };
                var pathways = new List<int> { config.pathway };

                if (config.IsAnySpeed()) speeds = new List<int> { 1, 2, 3 };
                if (config.IsAnyScene())
                {
                    scenes = new List<string>();

                    foreach (var scene in Singleton<StageBattleComponent>.instance.sceneInfo)
                    {
                        var noteType = config.GetNoteType();
                        if (noteType is NoteType.Hp or NoteType.Music && scene.Value == 8) continue;

                        scenes.Add($"scene_0{scene.Value}");
                    }
                }

                if (config.IsAnyPathway()) pathways = new List<int> { 0, 1 };

                foreach (var key in
                         from pathway in pathways
                         from speed in speeds
                         from scene in scenes
                         select GetNoteDataKey(config.ibms_id, pathway, speed, scene))
                    NoteData.TryAdd(key, config);
            }
        }

        private static string GetNoteDataKey(string bmsId, int pathway, int speed, string scene)
        {
            return $"{bmsId}-{pathway}-{speed}-{scene}";
        }
    }
}

