using System.Text.Json.Nodes;
using CustomAlbums.Utilities;
using Il2CppAssets.Scripts.GameCore.Managers;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppGameLogic;
using Il2CppPeroPeroGames.GlobalDefines;

namespace CustomAlbums.Data
{
    public class Bms
    {
        public enum BmsId
        {
            None,
            Small,
            SmallUp,
            SmallDown,
            Medium1,
            Medium1Up,
            Medium1Down,
            Medium2,
            Medium2Up,
            Medium2Down,
            Large1,
            Large2,
            Raider,
            Hammer,
            Gemini,
            Hold,
            Masher,
            Gear,
            UpsideDownRaider,
            UpsideDownHammer,
            Speed1Both,
            Speed2Both,
            Speed3Both,
            Speed1Low,
            Speed2Low,
            Speed3Low,
            Speed1High,
            Speed2High,
            Speed3High,
            Music,
            BossMelee1,
            BossMelee2,
            BossProjectile1,
            BossProjectile2,
            BossProjectile3,
            BossMasher1,
            BossMasher2,
            BossGear,
            BossEntrance,
            BossExit,
            BossReadyPhase1,
            BossEndPhase1,
            BossReadyPhase2,
            BossEndPhase2,
            BossSwapPhase12,
            BossSwapPhase21,
            HideNotes,
            UnhideNotes,
            HideBoss,
            UnhideBoss,
            SceneSwitchSpaceStation,
            SceneSwitchRetroCity,
            SceneSwitchCastle,
            SceneSwitchRainyNight,
            SceneSwitchOriental,
            SceneSwitchGrooveCoaster,
            SceneSwitchTouhou,
            SceneSwitchDjmax,
            SceneSwitchMiku,
            PItem,
            Ghost,
            Heart,
            Note,
            HideBackground,
            UnhideBackground,
            ScreenScrollUp,
            ScreenScrollDown,
            ScreenScrollOff,
            ScanlineRipplesOn,
            ScanlineRipplesOff,
            ChromaticAberrationOn,
            ChromaticAberrationOff,
            VignetteOn,
            VignetteOff,
            TvStaticOn,
            TvStaticOff,
            FlashbangStart,
            FlashbangMid,
            FlashbangEnd,
            BgStopOn,
            BgStopOff,
            MosaicOn,
            MosaicOff,
            SepiaOn,
            SepiaOff,
            MediumBullet,
            MediumBulletUp,
            MediumBulletDown,
            MediumBulletLaneShift,
            SmallBullet,
            SmallBulletUp,
            SmallBulletDown,
            SmallBulletLaneShift,
            LargeBullet,
            LargeBulletUp,
            LargeBulletDown,
            LargeBulletLaneShift,
            BossBullet1,
            BossBullet1LaneShift,
            BossBullet2,
            BossBullet2LaneShift
        }

        [Flags]
        public enum ChannelType
        {
            /// <summary>
            ///     Channel does not support anything.
            /// </summary>
            None = 0,

            /// <summary>
            ///     Channel supports the Ground Lane.
            /// </summary>
            Ground = 1,

            /// <summary>
            ///     Channel supports the Air Lane.
            /// </summary>
            Air = 2,

            /// <summary>
            ///     Channel supports standard events.
            /// </summary>
            Event = 4,

            /// <summary>
            ///     Channel supports scene events.
            /// </summary>
            Scene = 8,

            /// <summary>
            ///     Notes in this channel become Heart Enemies if possible.
            /// </summary>
            SpBlood = 16,

            /// <summary>
            ///     Notes in this channel become Tap Holds if possible.
            /// </summary>
            SpTapHolds = 32,

            /// <summary>
            ///     This channel is used for BPM changes, with the value directly present.
            /// </summary>
            SpBpmDirect = 64,

            /// <summary>
            ///     This channel is used for BPM changes, with the value placed in a lookup table.
            /// </summary>
            SpBpmLookup = 128,

            /// <summary>
            ///     This channel is used for time signature changes.
            /// </summary>
            SpTimesig = 256,

            /// <summary>
            ///     This channel is used for scroll speed changes.
            /// </summary>
            SpScroll = 512
        }

        public static readonly Dictionary<string, ChannelType> Channels = new()
        {
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

        public static readonly Dictionary<string, BmsId> BmsIds = new()
        {
            ["00"] = BmsId.None,
            ["01"] = BmsId.Small,
            ["02"] = BmsId.SmallUp,
            ["03"] = BmsId.SmallDown,
            ["04"] = BmsId.Medium1,
            ["05"] = BmsId.Medium1Up,
            ["06"] = BmsId.Medium1Down,
            ["07"] = BmsId.Medium2,
            ["08"] = BmsId.Medium2Up,
            ["09"] = BmsId.Medium2Down,
            ["0A"] = BmsId.Large1,
            ["0B"] = BmsId.Large2,
            ["0C"] = BmsId.Raider,
            ["0D"] = BmsId.Hammer,
            ["0E"] = BmsId.Gemini,
            ["0F"] = BmsId.Hold,
            ["0G"] = BmsId.Masher,
            ["0H"] = BmsId.Gear,
            ["0I"] = BmsId.UpsideDownRaider,
            ["0J"] = BmsId.UpsideDownHammer,
            ["0O"] = BmsId.Speed1Both,
            ["0P"] = BmsId.Speed2Both,
            ["0Q"] = BmsId.Speed3Both,
            ["0R"] = BmsId.Speed1Low,
            ["0S"] = BmsId.Speed2Low,
            ["0T"] = BmsId.Speed3Low,
            ["0U"] = BmsId.Speed1High,
            ["0V"] = BmsId.Speed2High,
            ["0W"] = BmsId.Speed3High,
            ["10"] = BmsId.Music,
            ["11"] = BmsId.BossMelee1,
            ["12"] = BmsId.BossMelee2,
            ["13"] = BmsId.BossProjectile1,
            ["14"] = BmsId.BossProjectile2,
            ["15"] = BmsId.BossProjectile3,
            ["16"] = BmsId.BossMasher1,
            ["17"] = BmsId.BossMasher2,
            ["18"] = BmsId.BossGear,
            ["1A"] = BmsId.BossEntrance,
            ["1B"] = BmsId.BossExit,
            ["1C"] = BmsId.BossReadyPhase1,
            ["1D"] = BmsId.BossEndPhase1,
            ["1E"] = BmsId.BossReadyPhase2,
            ["1F"] = BmsId.BossEndPhase2,
            ["1G"] = BmsId.BossSwapPhase12,
            ["1H"] = BmsId.BossSwapPhase21,
            ["1J"] = BmsId.HideNotes,
            ["1K"] = BmsId.UnhideNotes,
            ["1L"] = BmsId.HideBoss,
            ["1M"] = BmsId.UnhideBoss,
            ["1O"] = BmsId.SceneSwitchSpaceStation,
            ["1P"] = BmsId.SceneSwitchRetroCity,
            ["1Q"] = BmsId.SceneSwitchCastle,
            ["1R"] = BmsId.SceneSwitchRainyNight,
            ["1S"] = BmsId.SceneSwitchOriental,
            ["1T"] = BmsId.SceneSwitchGrooveCoaster,
            ["1U"] = BmsId.SceneSwitchTouhou,
            ["1V"] = BmsId.SceneSwitchDjmax,
            ["1X"] = BmsId.SceneSwitchMiku,
            ["20"] = BmsId.PItem,
            ["21"] = BmsId.Ghost,
            ["22"] = BmsId.Heart,
            ["23"] = BmsId.Note,
            ["25"] = BmsId.HideBackground,
            ["26"] = BmsId.UnhideBackground,
            ["27"] = BmsId.ScreenScrollUp,
            ["28"] = BmsId.ScreenScrollDown,
            ["29"] = BmsId.ScreenScrollOff,
            ["2A"] = BmsId.ScanlineRipplesOn,
            ["2B"] = BmsId.ScanlineRipplesOff,
            ["2C"] = BmsId.ChromaticAberrationOn,
            ["2D"] = BmsId.ChromaticAberrationOff,
            ["2E"] = BmsId.VignetteOn,
            ["2F"] = BmsId.VignetteOff,
            ["2G"] = BmsId.TvStaticOn,
            ["2H"] = BmsId.TvStaticOff,
            ["2I"] = BmsId.FlashbangStart,
            ["2J"] = BmsId.FlashbangMid,
            ["2K"] = BmsId.FlashbangEnd,
            ["2N"] = BmsId.BgStopOn,
            ["2O"] = BmsId.BgStopOff,
            ["2P"] = BmsId.MosaicOn,
            ["2Q"] = BmsId.MosaicOff,
            ["2R"] = BmsId.SepiaOn,
            ["2S"] = BmsId.SepiaOff,
            ["30"] = BmsId.MediumBullet,
            ["31"] = BmsId.MediumBulletUp,
            ["32"] = BmsId.MediumBulletDown,
            ["33"] = BmsId.MediumBulletLaneShift,
            ["34"] = BmsId.SmallBullet,
            ["35"] = BmsId.SmallBulletUp,
            ["36"] = BmsId.SmallBulletDown,
            ["37"] = BmsId.SmallBulletLaneShift,
            ["38"] = BmsId.LargeBullet,
            ["39"] = BmsId.LargeBulletUp,
            ["3A"] = BmsId.LargeBulletDown,
            ["3B"] = BmsId.LargeBulletLaneShift,
            ["3C"] = BmsId.BossBullet1,
            ["3D"] = BmsId.BossBullet1LaneShift,
            ["3E"] = BmsId.BossBullet2,
            ["3F"] = BmsId.BossBullet2LaneShift
        };

        public JsonObject Info { get; set; }
        public JsonArray Notes { get; set; }
        public JsonArray NotesPercent { get; set; }
        public string Md5 { get; set; }
        public Dictionary<string, NoteConfigData> NoteData { get; set; }

        public float Bpm
        {
            get
            {
                var bpmString = Info["BPM"]?.GetValue<string>() ?? Info["BPM01"]?.GetValue<string>() ?? string.Empty;
                return bpmString.TryParseAsFloat(out var bpm) ? bpm : 0f;
            }
        }

        public static string GetNoteDataKey(string bmsId, int pathway, int speed, string scene)
        {
            return $"{bmsId}-{pathway}-{speed}-{scene}";
        }

        public void InitNoteData()
        {
            NoteData = new Dictionary<string, NoteConfigData>();

            // NOTE: PPG has officially gone mad. The new scene 12 does not appear in the scene list, so we can't really use the scene list anymore.
            Span<int> sceneSpan = stackalloc int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

            foreach (var config in NoteDataMananger.instance.noteDatas)
            {
                // Ignore april fools variants (these are handled elsewhere)
                if (config.IsAprilFools()) continue;
                // Ignore phase 2 boss gears
                if (config.IsPhase2BossGear()) continue;

                // Scene setting of "0" is a wildcard
                var anyScene = config.IsAnyScene();
                // Notes with these values are extremely likely to be events, and get registered to all pathways
                var anyPathway = config.IsAnyPathway();
                // Boss type, None type, boss mashers, and events get registered to all speeds
                var anySpeed = config.IsAnySpeed();

                var speeds = new List<int> { config.speed };
                var scenes = new List<string> { config.scene };
                var pathways = new List<int> { config.pathway };

                // Use all speeds if any speed
                if (anySpeed) speeds = new List<int> { 1, 2, 3 };
                // Use all scenes if any scene
                if (anyScene)
                {
                    scenes = new List<string>();

                    foreach (var scene in sceneSpan)
                    {
                        // Special handling for collectibles in touhou scene
                        if (config.GetNoteType() is NoteType.Hp or NoteType.Music && scene == 8)
                            continue;

                        var sceneSuffix = scene.ToString().PadLeft(2, '0');
                        scenes.Add($"scene_{sceneSuffix}");
                    }
                }

                // Use all pathways if any pathway
                if (anyPathway) pathways = new List<int> { 0, 1 };

                foreach (var key in pathways.SelectMany(pathway => speeds.SelectMany(speed =>
                             scenes.Select(scene => GetNoteDataKey(config.ibms_id, pathway, speed, scene)))))
                    NoteData.TryAdd(key, config);
            }
        }

        public Il2CppSystem.Collections.Generic.List<SceneEvent> GetSceneEvents()
        {
            var sceneEvents = new Il2CppSystem.Collections.Generic.List<SceneEvent>();

            foreach (var note in Notes)
            {
                var bmsKey = note["value"]?.GetValue<string>() ?? string.Empty;
                if (string.IsNullOrEmpty(bmsKey)) continue;

                var channelTone = note["tone"]?.GetValue<string>() ?? string.Empty;
                if (!Channels.TryGetValue(channelTone, out var channel)) continue;

                if (channel.HasFlag(ChannelType.Scene))
                    sceneEvents.Add(new SceneEvent
                    {
                        time = note["time"].GetValueAsIl2CppDecimal(),
                        uid = $"SceneEvent/{bmsKey}"
                    });
                else if (channel.HasFlag(ChannelType.SpBpmDirect) || channel.HasFlag(ChannelType.SpBpmLookup))
                    sceneEvents.Add(new SceneEvent
                    {
                        time = note["time"].GetValueAsIl2CppDecimal(),
                        uid = "SceneEvent/OnBPMChanged",
                        value = note["value"]?.GetValue<string>() ?? string.Empty
                    });
            }

            return sceneEvents;
        }

        public JsonArray GetNoteData()
        {
            if (NoteData is null || NoteData.Count == 0) InitNoteData();
            var processed = new JsonArray();

            var speedAir = (Info["PLAYER"]?.GetValue<string>() ?? "1").ParseAsInt();
            var speedGround = speedAir;

            var objectId = 1;

            for (var i = 0; i < Notes.Count; i++)
            {
                var note = Notes[i];
                if (note is null) continue;

                var bmsKey = note["value"]?.GetValue<string>() ?? "00";
                var bmsId = BmsIds.GetValueOrDefault(bmsKey, BmsId.None);
                var channel = note["tone"]?.GetValue<string>() ?? string.Empty;
                var channelType = Channels.GetValueOrDefault(channel, ChannelType.None);

                // Handle lane type
                var pathway = -1;
                if (channelType.HasFlag(ChannelType.Air))
                    pathway = 1;
                else if (channelType.HasFlag(ChannelType.Ground) || channelType.HasFlag(ChannelType.Event))
                    pathway = 0;

                if (pathway == -1) continue;

                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (bmsId)
                {
                    // Handle speed changes
                    case BmsId.Speed1Both:
                        speedGround = 1;
                        speedAir = 1;
                        break;
                    case BmsId.Speed2Both:
                        speedGround = 2;
                        speedAir = 2;
                        break;
                    case BmsId.Speed3Both:
                        speedGround = 3;
                        speedAir = 3;
                        break;
                    case BmsId.Speed1Low:
                        speedGround = 1;
                        break;
                    case BmsId.Speed1High:
                        speedAir = 1;
                        break;
                    case BmsId.Speed2Low:
                        speedGround = 2;
                        break;
                    case BmsId.Speed2High:
                        speedAir = 2;
                        break;
                    case BmsId.Speed3Low:
                        speedGround = 3;
                        break;
                    case BmsId.Speed3High:
                        speedAir = 3;
                        break;
                }

                var speed = pathway == 1 ? speedAir : speedGround;
                var scene = Info["GENRE"]?.GetValue<string>();

                if (!NoteData!.TryGetValue(GetNoteDataKey(bmsKey, pathway, speed, scene), out var configData))
                    continue;

                var time = note["time"]?.GetValueAsDecimal() ?? 0M;

                // Hold note & masher 
                var holdLength = 0M;
                var isHold = configData.GetNoteType() is NoteType.Press or NoteType.Mul;
                if (isHold)
                {
                    if (channelType.HasFlag(ChannelType.SpTapHolds))
                        holdLength = 0.001M;
                    else
                        for (var j = i + 1; j < Notes.Count; j++)
                        {
                            var holdEndNote = Notes[j];
                            var holdEndTime = holdEndNote?["time"]?.GetValueAsDecimal() ?? 0M;
                            var holdEndBmsKey = holdEndNote?["value"]?.GetValue<string>() ?? string.Empty;
                            var holdEndChannel = holdEndNote?["tone"]?.GetValue<string>() ?? string.Empty;

                            if (holdEndBmsKey != bmsKey || holdEndChannel != channel) continue;
                            holdLength = holdEndTime - time;
                            Notes[j]!["value"] = "";
                            break;
                        }
                }

                processed.Add(new JsonObject
                {
                    ["id"] = objectId++,
                    ["time"] = time,
                    ["note_uid"] = configData.uid,
                    ["length"] = holdLength,
                    ["pathway"] = pathway,
                    ["blood"] = !isHold && channelType.HasFlag(ChannelType.SpBlood)
                });
            }

            return processed;
        }
    }
}