using System.Globalization;
using System.Text.Json.Nodes;
using CustomAlbums.Data;
using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using Il2Cpp;
using Il2CppAssets.Scripts.GameCore;
using Il2CppAssets.Scripts.GameCore.Managers;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.Managers;
using Il2CppFormulaBase;
using Il2CppGameLogic;
using Il2CppPeroPeroGames.GlobalDefines;
using Il2CppPeroTools2.Resources;
using Il2CppSpine.Unity;
using UnityEngine;
using static CustomAlbums.Data.BmsStates;
using Animation = Il2CppSpine.Animation;
using Decimal = Il2CppSystem.Decimal;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums
{
    internal static class BmsLoader
    {
        private static readonly Dictionary<string, NoteConfigData> NoteData = new();
        private static readonly Dictionary<string, Dictionary<string, NoteConfigData>> BossData = new();
        private static Decimal _delay;
        private static readonly Logger Logger = new(nameof(BmsLoader));

        /// <summary>
        ///     Creates a Bms object from a BMS file.
        /// </summary>
        /// <param name="stream">MemoryStream of BMS file.</param>
        /// <param name="bmsName">Name of BMS score.</param>
        /// <returns>Loaded Bms object.</returns>
        internal static Bms Load(Stream stream, string bmsName)
        {
            Logger.Msg($"Loading bms {bmsName}...");

            var bpmDict = new Dictionary<string, float>();
            var notePercents = new Dictionary<int, JsonObject>();
            var dataList = new List<JsonObject>();
            var notesArray = new JsonArray();
            var info = new JsonObject();

            using var streamReader = new StreamReader(stream);
            while (streamReader.ReadLine()?.Trim() is { } line)
            {
                if (string.IsNullOrEmpty(line) || !line.StartsWith("#")) continue;

                // Remove # from beginning of line
                line = line[1..];

                if (line.Contains(' '))
                {
                    // Parse header
                    var split = line.Split(' ');
                    var key = split[0];
                    var value = split[1];

                    info[key] = value;

                    if (!key.Contains("BPM")) continue;

                    var bpmKey = string.IsNullOrEmpty(key[3..]) ? "00" : key[3..];
                    bpmDict.Add(bpmKey, float.Parse(value, CultureInfo.InvariantCulture));

                    if (bpmKey != "00") continue;

                    var freq = 60f / float.Parse(value, CultureInfo.InvariantCulture) * 4f;
                    var obj = new JsonObject
                    {
                        { "tick", 0f },
                        { "freq", freq }
                    };
                    dataList.Add(obj);
                }
                else if (line.Contains(':'))
                {
                    // Parse data field
                    var split = line.Split(':');
                    var key = split[0];
                    var value = split[1];

                    var beat = int.Parse(key[..3], CultureInfo.InvariantCulture);
                    var typeCode = key.Substring(3, 2);
                    
                    if (!Bms.Channels.ContainsKey(typeCode)) continue;
                    
                    var type = Bms.Channels[typeCode];

                    if (type is Bms.ChannelType.SpTimesig)
                    {
                        var obj = new JsonObject
                        {
                            { "beat", beat },
                            { "percent", float.Parse(value, CultureInfo.InvariantCulture) }
                        };
                        notePercents.Add(beat, obj);
                    }
                    else
                    {
                        var objLength = value.Length / 2;
                        for (var i = 0; i < objLength; i++)
                        {
                            var note = value.Substring(i * 2, 2);
                            if (note is "00") continue;

                            var tick = (float)i / objLength + beat;

                            if (type is Bms.ChannelType.SpBpmDirect or Bms.ChannelType.SpBpmLookup)
                            {
                                // Handle BPM changes
                                var freqDivide = type == Bms.ChannelType.SpBpmLookup &&
                                                 bpmDict.TryGetValue(note, out var bpm)
                                    ? bpm
                                    : Convert.ToInt32(note, 16);
                                var freq = 60f / freqDivide * 4f;

                                var obj = new JsonObject
                                {
                                    { "tick", tick },
                                    { "freq", freq }
                                };
                                dataList.Add(obj);
                                dataList.Sort((l, r) =>
                                {
                                    var tickL = l["tick"].GetValue<float>();
                                    var tickR = r["tick"].GetValue<float>();

                                    return tickR.CompareTo(tickL);
                                });
                            }
                            else
                            {
                                // Parse other note data
                                var time = 0f; // num3
                                var totalOffset = 0f; // num4

                                var data = dataList.FindAll(d => d["tick"].GetValue<float>() < tick);
                                for (var j = data.Count - 1; j >= 0; j--)
                                {
                                    var obj = data[j];
                                    var offset = 0f; // num5
                                    var freq = obj["freq"].GetValue<float>(); // num6

                                    if (j - 1 >= 0)
                                    {
                                        var prevObj = data[j - 1];
                                        offset = prevObj["tick"].GetValue<float>() - obj["tick"].GetValue<float>();
                                    }

                                    if (j == 0) offset = tick - obj["tick"].GetValue<float>();

                                    var localOffset = totalOffset; // num7
                                    totalOffset += offset;
                                    var floorOffset = Mathf.FloorToInt(localOffset); // num8
                                    var ceilOffset = Mathf.CeilToInt(totalOffset); // num9

                                    for (var k = floorOffset; k < ceilOffset; k++)
                                    {
                                        var off = 1f; // num10

                                        if (k == floorOffset)
                                            off = k + 1 - localOffset;
                                        if (k == ceilOffset - 1)
                                            off = totalOffset - (ceilOffset - 1);
                                        if (ceilOffset == floorOffset + 1)
                                            off = totalOffset - localOffset;

                                        notePercents.TryGetValue(k, out var node);
                                        var percent = node?["percent"].GetValue<float>() ?? 1f;
                                        time += Mathf.RoundToInt(off * percent * freq / 1E-06f) * 1E-06F;
                                    }
                                }

                                var noteObj = new JsonObject
                                {
                                    { "time", time },
                                    { "value", note },
                                    { "tone", typeCode }
                                };
                                notesArray.Add(noteObj);
                            }
                        }
                    }
                }
            }

            var list = notesArray.ToList();
            list.Sort((l, r) =>
            {
                var lTime = l["time"]!.GetValue<float>();
                var rTime = r["time"]!.GetValue<float>();
                var lTone = l["tone"]!.GetValue<string>();
                var rTone = r["tone"]!.GetValue<string>();

                // Accurate for note sorting up to 6 decimal places
                var lScore = (long)(lTime * 1000000) * 10 + (lTone == "15" ? 0 : 1);
                var rScore = (long)(rTime * 1000000) * 10 + (rTone == "15" ? 0 : 1);

                return Math.Sign(lScore - rScore);
            });

            notesArray.Clear();
            list.ForEach(notesArray.Add);

            var percentsArray = new JsonArray();
            notePercents.Values.ToList().ForEach(percentsArray.Add);
            var bms = new Bms
            {
                Info = info,
                Notes = notesArray,
                NotesPercent = percentsArray,
                Md5 = stream.GetHash()
            };
            bms.Info["NAME"] = bmsName;
            bms.Info["NEW"] = true;

            if (bms.Info.TryGetPropertyValue("BANNER", out var banner))
                bms.Info["BANNER"] = "cover/" + banner;
            else
                bms.Info["BANNER"] = "cover/none_cover.png";

            Logger.Msg($"Loaded bms {bmsName}.");

            return bms;
        }

        /// <summary>
        ///     Transmutes Bms data into StageInfo data.
        /// </summary>
        /// <param name="bms">The Bms object to transmute.</param>
        /// <returns>The transmuted StageInfo object.</returns>
        internal static StageInfo TransmuteData(Bms bms)
        {
            if (NoteData.Count == 0) InitNoteData();
            MusicDataManager.Clear();
            _delay = 0;

            var noteData = bms.GetNoteData();
            Logger.Msg("Got note data");

            LoadMusicData(noteData);
            MusicDataManager.Sort();

            ProcessBossData(bms);
            ProcessDelay(bms);
            MusicDataManager.Sort();

            ProcessGeminis();

            // Process the delay for each MusicData
            foreach (var mData in MusicDataManager.Data)
            {
                mData.tick -= _delay;
                mData.showTick = Decimal.Round(mData.tick - mData.dt, 2);
                if (mData.isLongPressType)
                    mData.endIndex -= (int)(_delay / (Decimal)0.001f);
            }

            // Transmute the MusicData to a new StageInfo object
            var stageInfo = ScriptableObject.CreateInstance<StageInfo>();
            stageInfo.musicDatas = new Il2CppSystem.Collections.Generic.List<MusicData>();
            foreach (var musicData in MusicDataManager.Data)
                stageInfo.musicDatas.Add(musicData);
            stageInfo.delay = _delay;

            MusicDataManager.Clear();
            return stageInfo;
        }

        private static void InitNoteData()
        {
            foreach (var nData in SingletonScriptableObject<NoteDataMananger>.instance.noteDatas)
            {
                NoteData.TryAdd(nData.uid, nData);
                NoteData.TryAdd(Bms.GetNoteDataKey(nData.ibms_id, nData.pathway, nData.speed, nData.scene), nData);

                if (nData.GetNoteType() != NoteType.None || string.IsNullOrEmpty(nData.boss_action) ||
                    nData.boss_action == "0") continue;

                BossData.TryAdd(nData.scene, new Dictionary<string, NoteConfigData>());
                BossData[nData.scene].TryAdd(nData.boss_action, nData);
            }
        }

        private static void LoadMusicData(JsonArray noteData)
        {
            short noteId = 1;
            foreach (var node in noteData)
            {
                if (noteId == short.MaxValue)
                {
                    Logger.Warning(
                        $"Cannot process full chart, there are too many objects. Max objects is {short.MaxValue}.");
                    break;
                }

                var configData = node.ToMusicConfigData();
                if (configData.time < 0) continue;

                // Create a new note for each configData
                var newNote = Interop.CreateTypeValue<MusicData>();
                newNote.objId = noteId++;
                newNote.tick = Decimal.Round(configData.time, 3);
                newNote.configData = configData;
                newNote.isLongPressEnd = false;
                newNote.isLongPressing = false;

                if (NoteData.TryGetValue(newNote.configData.note_uid, out var newNoteData))
                    newNote.noteData = newNoteData;

                MusicDataManager.Add(newNote);

                // Create ticks for hold notes. If it isn't a hold note, there is no need to continue.
                if (!newNote.isLongPressStart) continue;

                // Calculate the index in which the hold note ends
                var endIndex = (int)(Decimal.Round(
                    newNote.tick + newNote.configData.length - newNote.noteData.left_great_range -
                    newNote.noteData.left_perfect_range,
                    3) / (Decimal)0.001f);

                for (var i = 1; i <= newNote.longPressCount; i++)
                {
                    var holdTick = Interop.CreateTypeValue<MusicData>();
                    holdTick.objId = noteId++;
                    holdTick.tick = i == newNote.longPressCount
                        ? newNote.tick + newNote.configData.length
                        : newNote.tick + (Decimal)0.1f * i;
                    holdTick.configData = newNote.configData;

                    // ACTUALLY REQUIRED TO WORK
                    var dataCopy = holdTick.configData;
                    dataCopy.length = 0;
                    holdTick.configData = dataCopy;

                    holdTick.isLongPressing = i != newNote.longPressCount;
                    holdTick.isLongPressEnd = i == newNote.longPressCount;
                    holdTick.noteData = newNote.noteData;
                    holdTick.longPressPTick = newNote.configData.time;
                    holdTick.endIndex = endIndex;

                    MusicDataManager.Add(holdTick);
                }
            }

            Logger.Msg("Loaded music data!");
        }

        private static void ProcessBossData(Bms bms)
        {
            var scene = bms.Info["GENRE"]?.GetValue<string>() ?? string.Empty;
            var bossData = MusicDataManager.Data.Where(mData => mData.isBossNote).ToList();

            // If the boss is not used for some reason, no need to process animations.
            if (bossData.Count == 0) return;

            // Add a boss exit animation if it is missing.
            var finalData = bossData[^1];
            if (AnimStatesRight[finalData.noteData.boss_action] != BossState.OffScreen)
            {
                var finalNote = MusicDataManager.Data[^1];
                var tick = finalNote.tick;

                if (finalNote.isBossNote)
                {
                    var startDelay = Decimal.Round((Decimal)GetStartDelay(finalNote.noteData.prefab_name), 3);
                    var duration = (Decimal)GetAnimationDuration(scene, finalNote.noteData.boss_action);
                    tick += Decimal.Round(-startDelay + duration, 3);
                }

                tick = Decimal.Round(tick + (Decimal)0.1f, 3);

                var exitNoteData = BossData[scene]["out"];

                var exitConfig = Interop.CreateTypeValue<MusicConfigData>();
                exitConfig.note_uid = exitNoteData.uid;
                exitConfig.time = tick;

                var exitMusicData = Interop.CreateTypeValue<MusicData>();
                exitMusicData.objId = 0;
                exitMusicData.tick = exitConfig.time;
                exitMusicData.configData = exitConfig;
                exitMusicData.noteData = exitNoteData;

                MusicDataManager.Add(exitMusicData);
                bossData.Add(exitMusicData);
                Logger.Msg("Added missing boss exit at " + exitConfig.time);
            }

            // Fix incorrect phase gears
            var phaseGearConfig = Interop.CreateTypeValue<NoteConfigData>();
            phaseGearConfig.ibms_id = "";

            for (var i = 0; i < bossData.Count; i++)
            {
                var data = bossData[i];

                if (data.noteData.GetNoteType() != NoteType.Block) continue;

                // Find the next boss animation that is not a gear
                var bossAnimAhead = Interop.CreateTypeValue<MusicData>();
                bossAnimAhead.configData.time = Decimal.MinValue;

                for (var j = i + 1; j < bossData.Count; j++)
                {
                    var dataAhead = bossData[j];
                    if (dataAhead.noteData.GetNoteType() == NoteType.Block) continue;

                    bossAnimAhead = dataAhead;
                    break;
                }

                MusicData bossAnimBefore;
                if (i > 0)
                {
                    bossAnimBefore = bossData[i - 1];
                }
                else
                {
                    bossAnimBefore = Interop.CreateTypeValue<MusicData>();
                    bossAnimBefore.configData.time = Decimal.MinValue;
                }

                var diffToAhead = Math.Abs((float)data.configData.time - (float)bossAnimAhead.configData.time);
                var diffToBefore = Math.Abs((float)data.configData.time - (float)bossAnimBefore.configData.time);
                var ahead = diffToAhead < diffToBefore;

                var stateBehind = i > 0 ? AnimStatesRight[bossAnimBefore.noteData.boss_action] : BossState.OffScreen;
                var stateAhead = AnimStatesLeft.TryGetValue(bossAnimAhead.noteData.boss_action, out var state)
                    ? state
                    : BossState.OffScreen;
                var usedState = ahead ? stateAhead : stateBehind;
                var correctState = usedState is BossState.Phase1 or BossState.Phase2;
                if (!correctState)
                {
                    ahead = !ahead;
                    usedState = ahead ? stateAhead : stateBehind;
                    correctState = usedState is BossState.Phase1 or BossState.Phase2;
                }

                if (!correctState) continue;
                if (usedState is not (BossState.Phase1 or BossState.Phase2)) continue;

                if ((ahead && AnimStatesLeft[data.noteData.boss_action] == usedState) ||
                    (!ahead && AnimStatesRight[data.noteData.boss_action] == usedState))
                    continue;

                var phase = usedState == BossState.Phase1 ? 1 : 2;

                var noteData = SingletonScriptableObject<NoteDataMananger>.instance.noteDatas;
                if (phaseGearConfig.ibms_id != data.noteData.ibms_id
                    || phaseGearConfig.pathway != data.noteData.pathway
                    || phaseGearConfig.scene != data.noteData.scene
                    || phaseGearConfig.speed != data.noteData.speed
                    || !phaseGearConfig.boss_action.StartsWith($"boss_far_atk_{phase}"))
                    foreach (var d in noteData)
                    {
                        if (d.ibms_id != data.noteData.ibms_id
                            || d.pathway != data.noteData.pathway
                            || d.scene != data.noteData.scene
                            || d.speed != data.noteData.speed
                            || !d.boss_action.StartsWith($"boss_far_atk_{phase}")) continue;

                        phaseGearConfig = d;
                        break;
                    }

                var fixedConfigData = Interop.CreateTypeValue<MusicConfigData>();
                fixedConfigData.blood = data.configData.blood;
                fixedConfigData.id = data.configData.id;
                fixedConfigData.length = data.configData.length;
                fixedConfigData.note_uid = phaseGearConfig.uid;
                fixedConfigData.pathway = data.configData.pathway;
                fixedConfigData.time = data.configData.time;

                var fixedGear = Interop.CreateTypeValue<MusicData>();
                fixedGear.objId = data.objId;
                fixedGear.tick = data.tick;
                fixedGear.configData = fixedConfigData;
                fixedGear.isLongPressEnd = data.isLongPressEnd;
                fixedGear.isLongPressing = data.isLongPressing;
                fixedGear.noteData = phaseGearConfig;

                MusicDataManager.Set(fixedGear.objId, fixedGear);
                bossData[i] = fixedGear;
                Logger.Msg($"Fixed gear at tick {data.tick}.");
            }

            // Resolve state changes in list
            var bossState = BossState.OffScreen;
            for (var i = 0; i < bossData.Count; i++)
            {
                var anim = bossData[i].noteData.boss_action;
                var nextState = AnimStatesLeft[anim];

                if (bossState != nextState)
                {
                    var transfer = StateTransferAnims[bossState][nextState];
                    var transferNoteData = BossData[scene][transfer];
                    var alignment = TransferAlignment[transfer];

                    var alignData = alignment == AnimAlignment.Right ? bossData[i] : bossData[i - 1];

                    var rightDelay = (Decimal)GetStartDelay(bossData[i].noteData.prefab_name);
                    var leftDelay = i == 0 ? 0 : (Decimal)GetStartDelay(bossData[i - 1].noteData.prefab_name);
                    var alignDelay = alignment == AnimAlignment.Left ? leftDelay : rightDelay;

                    var duration = (Decimal)GetAnimationDuration(scene, transfer);

                    var mConfig = Interop.CreateTypeValue<MusicConfigData>();
                    mConfig.note_uid = transferNoteData.uid;
                    mConfig.time = Decimal.Round(alignData.tick - alignDelay - duration * (int)alignment, 3);

                    var mData = Interop.CreateTypeValue<MusicData>();
                    mData.tick = mConfig.time;
                    mData.configData = mConfig;
                    mData.noteData = transferNoteData;

                    var tolerance = (Decimal)0.300f;
                    var fits = alignment switch
                    {
                        AnimAlignment.Left => mData.tick + duration < bossData[i].tick - rightDelay + tolerance,
                        _ => i == 0 || mData.tick > bossData[i - 1].tick - leftDelay
                    };

                    if (!fits)
                    {
                        bossState = AnimStatesRight[bossData[i].noteData.boss_action];
                        continue;
                    }

                    MusicDataManager.Add(mData);
                    bossData.Insert(i, mData);
                    i--;
                }
                else
                {
                    bossState = AnimStatesRight[bossData[i].noteData.boss_action];
                }
            }

            Logger.Msg("Processed boss animations!");
        }

        private static void ProcessDelay(Bms bms)
        {
            var scene = bms.Info["GENRE"]?.GetValue<string>() ?? string.Empty;
            var sceneIndex = int.Parse(scene.Split('_')[1], CultureInfo.InvariantCulture);
            var sceneInfo = Singleton<StageBattleComponent>.instance.sceneInfo;
            var delayCache = new Dictionary<string, Decimal>();

            for (var i = 0; i < MusicDataManager.Data.Count; i++)
            {
                var mData = MusicDataManager.Data[i];
                if (!string.IsNullOrEmpty(mData.noteData.ibms_id))
                {
                    var type = mData.noteData.GetNoteType();
                    if (type == NoteType.SceneChange) sceneIndex = sceneInfo[mData.noteData.ibms_id];

                    var prefabName = mData.noteData.prefab_name;
                    if (!string.IsNullOrEmpty(prefabName))
                    {
                        // If not a pickup type, convert to most recent scene
                        if (type != NoteType.Hp && type != NoteType.Music)
                        {
                            var prefix = prefabName[..2];
                            if (!new[] { "00", "em", "bo" }.Contains(prefix))
                                prefabName = prefabName.Remove(0, 2).Insert(0, $"{sceneIndex:D2}");
                        }

                        if (!delayCache.ContainsKey(prefabName))
                        {
                            var gameObject = ResourcesManager.instance.LoadFromName<GameObject>(prefabName);

                            if (gameObject != null)
                            {
                                var spineActionController = gameObject.GetComponent<SpineActionController>();
                                delayCache[prefabName] = (Decimal)spineActionController.startDelay;
                            }
                        }

                        if (delayCache.TryGetValue(prefabName, out var delay))
                        {
                            mData.dt = delay;
                            MusicDataManager.Set(i, mData);
                        }
                    }
                }

                var showTick = mData.tick - mData.dt;
                _delay = showTick < _delay ? showTick : _delay;
            }

            // Round delay
            _delay = Decimal.Round(_delay, 3);
            Logger.Msg("Processed delay!");
        }

        private static void ProcessGeminis()
        {
            var geminiCache = new Dictionary<Decimal, List<MusicData>>();

            for (var i = 1; i < MusicDataManager.Data.Count; i++)
            {
                var mData = MusicDataManager.Data[i];
                mData.doubleIdx = -1;
                MusicDataManager.Set(i, mData);

                if (mData.noteData.GetNoteType() != NoteType.Monster && mData.noteData.GetNoteType() != NoteType.Hide)
                    continue;

                if (geminiCache.TryGetValue(mData.tick, out var geminiList))
                {
                    var isNoteGemini = Bms.BmsIds[mData.noteData.ibms_id ?? "00"] == Bms.BmsId.Gemini;
                    var isTargetGemini = false;
                    var target = Interop.CreateTypeValue<MusicData>();

                    foreach (var gemini in geminiList.Where(gemini => mData.isAir != gemini.isAir))
                    {
                        target = gemini;
                        isTargetGemini = Bms.BmsIds[gemini.noteData.ibms_id ?? "00"] == Bms.BmsId.Gemini;

                        if (isNoteGemini && isTargetGemini) break;
                        if (!isNoteGemini) break;
                    }

                    if (target.objId > 0)
                    {
                        mData.isDouble = isNoteGemini && isTargetGemini;
                        mData.doubleIdx = target.objId;
                        target.isDouble = isNoteGemini && isTargetGemini;
                        target.doubleIdx = mData.objId;

                        MusicDataManager.Set(mData.objId, mData);
                        MusicDataManager.Set(target.objId, target);
                    }
                }
                else
                {
                    geminiCache[mData.tick] = new List<MusicData>();
                }

                geminiCache[mData.tick].Add(mData);
            }

            Logger.Msg("Processed geminis!");
        }

        private static float GetStartDelay(string prefab)
        {
            return ResourcesManager.instance.LoadFromName<GameObject>(prefab).GetComponent<SpineActionController>()
                .startDelay;
        }

        private static float GetAnimationDuration(string scene, string animation)
        {
            var resourceName =
                Singleton<ConfigManager>.instance.GetConfigStringValue("boss", "scene_name", "boss_name", scene);
            var controller = ResourcesManager.instance.LoadFromName<GameObject>(resourceName)
                .GetComponent<SpineActionController>();
            var animations = controller.gameObject.GetComponent<SkeletonAnimation>().skeletonDataAsset
                .GetSkeletonData(true).Animations;

            var arr = new SkeletActionData[controller.actionData.Count];
            controller.actionData.CopyTo(arr, 0);
            var actionData = new List<SkeletActionData>(arr).Find(dd => dd.name == animation);
            var animName = animation;
            if (actionData is { actionIdx: not null } && actionData.actionIdx.Length != 0)
                animName = actionData.actionIdx[0];
            return animations.Find((Il2CppSystem.Predicate<Animation>)((Animation a) => a.Name == animName)).Duration;
        }
    }
}