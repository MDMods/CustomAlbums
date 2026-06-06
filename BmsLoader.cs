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
using System.Diagnostics;
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

        private static readonly Dictionary<string, float> StartDelayCache = new();
        private static readonly Dictionary<(string scene, string animation), float> AnimationDurationCache = new();

        private static readonly HashSet<string> SceneAgnosticPrefixes = new() { "00", "em", "bo" };

        /// <summary>
        ///     Creates a Bms object from a BMS file.
        /// </summary>
        /// <param name="stream">MemoryStream of BMS file.</param>
        /// <param name="bmsName">Name of BMS score.</param>
        /// <returns>Loaded Bms object, or null if critical data is missing.</returns>
        internal static Bms Load(Stream stream, string bmsName)
        {
            Logger.Msg($"Loading bms {bmsName}...");

            var bpmDict = new Dictionary<string, float>();
            var timeSigEntries = new Dictionary<int, TimeSigEntry>();
            var bpmEntries = new List<BpmEntry>();
            var rawNotes = new List<RawNote>();
            var info = new BmsInfo();

            using var streamReader = new StreamReader(stream);
            while (streamReader.ReadLine()?.Trim() is { } line)
            {
                if (string.IsNullOrEmpty(line) || !line.StartsWith("#")) continue;

                // Remove # from beginning of line
                line = line[1..];

                if (line.Contains(' '))
                {
                    // Parse header
                    var spaceIndex = line.IndexOf(' ');
                    var key = line[..spaceIndex];
                    var value = line[(spaceIndex + 1)..];

                    // Skip WAV definition
                    if (key.StartsWith("WAV")) continue;

                    // Map known headers to typed properties
                    if (!ParseHeader(info, key, value, bpmDict, bpmEntries))
                    {
                        // Store unrecognized headers in fallback dictionary
                        info.Extra[key] = value;
                    }
                }
                else if (line.Contains(':'))
                {
                    // Parse data field
                    var colonIndex = line.IndexOf(':');
                    if (colonIndex < 5)
                    {
                        Logger.Warning($"Malformed data line (key too short): #{line}");
                        continue;
                    }

                    var key = line[..colonIndex];
                    var value = line[(colonIndex + 1)..];

                    if (!key[..3].TryParseAsInt(out var beat))
                    {
                        Logger.Warning($"Malformed measure number in: #{line}");
                        continue;
                    }

                    var typeCode = key.Substring(3, 2);

                    if (!Bms.Channels.TryGetValue(typeCode, out var type)) continue;

                    if (type is Bms.ChannelType.SpTimesig)
                    {
                        if (value.TryParseAsFloat(out var percent))
                            timeSigEntries[beat] = new TimeSigEntry(beat, percent);
                        else
                            Logger.Warning($"Invalid time signature value: {value}");
                    }
                    else
                    {
                        if (value.Length % 2 != 0)
                        {
                            Logger.Warning($"Odd-length data field (must be pairs of 2 chars): #{line}");
                            continue;
                        }

                        var objLength = value.Length / 2;
                        for (var i = 0; i < objLength; i++)
                        {
                            var note = value.Substring(i * 2, 2);
                            if (note is "00") continue;

                            var tick = (float)i / objLength + beat;

                            if (type is Bms.ChannelType.SpBpmDirect or Bms.ChannelType.SpBpmLookup)
                            {
                                // Handle BPM changes
                                float freqDivide;
                                if (type == Bms.ChannelType.SpBpmLookup && bpmDict.TryGetValue(note, out var bpm))
                                {
                                    freqDivide = bpm;
                                }
                                else
                                {
                                    try
                                    {
                                        freqDivide = Convert.ToInt32(note, 16);
                                    }
                                    catch (FormatException)
                                    {
                                        Logger.Warning($"Invalid hex BPM value: {note}");
                                        continue;
                                    }
                                }

                                var freq = 60f / freqDivide * 4f;
                                bpmEntries.Add(new BpmEntry(tick, freq));
                            }
                            else
                            {
                                var noteObj = new RawNote(
                                    Time: CalculateNoteTime(tick, bpmEntries, timeSigEntries),
                                    Value: note,
                                    Tone: typeCode
                                );
                                rawNotes.Add(noteObj);
                            }
                        }
                    }
                }
            }

            // Validate required headers
            if (string.IsNullOrEmpty(info.Genre))
                Logger.Warning($"BMS '{bmsName}' is missing required GENRE header.");
            if (info.Bpm <= 0)
                Logger.Warning($"BMS '{bmsName}' has no valid BPM.");

            // Sort notes by time, with events (channel "15") sorted before notes at the same timestamp
            rawNotes.Sort((l, r) =>
            {
                // Accurate for note sorting up to 6 decimal places
                var lScore = (long)(l.Time * 1000000) * 10 + (l.Tone == "15" ? 0 : 1);
                var rScore = (long)(r.Time * 1000000) * 10 + (r.Tone == "15" ? 0 : 1);
                return lScore.CompareTo(rScore);
            });

            var bms = new Bms
            {
                Info = info,
                Notes = rawNotes,
                NotesPercent = timeSigEntries.Values.ToList(),
                Md5 = stream.GetHash()
            };
            bms.Info.Name = bmsName;

            if (!string.IsNullOrEmpty(info.Banner))
                bms.Info.Banner = "cover/" + info.Banner;

            Logger.Msg($"Loaded bms {bmsName}.");

            return bms;
        }

        /// <summary>
        ///     Parses a known BMS header into the <see cref="BmsInfo"/> object.
        /// </summary>
        /// <returns><c>true</c> if the header was recognized and parsed, <c>false</c> otherwise.</returns>
        private static bool ParseHeader(BmsInfo info, string key, string value,
            Dictionary<string, float> bpmDict, List<BpmEntry> bpmEntries)
        {
            // Handle BPMxx headers (BPM, BPM01, BPM02, etc.)
            if (key.StartsWith("BPM"))
            {
                if (!value.TryParseAsFloat(out var bpmValue))
                {
                    Logger.Warning($"Invalid BPM value: {value}");
                    return true; // Recognized but invalid
                }

                var bpmKey = key.Length > 3 ? key[3..] : "00";
                bpmDict[bpmKey] = bpmValue;

                if (bpmKey == "00")
                {
                    info.Bpm = bpmValue;
                    var freq = 60f / bpmValue * 4f;
                    bpmEntries.Add(new BpmEntry(0f, freq));
                }

                return true;
            }

            switch (key)
            {
                case "PLAYER":
                    if (value.TryParseAsInt(out var player)) info.Player = player;
                    return true;
                case "GENRE":
                    info.Genre = value;
                    return true;
                case "TITLE":
                    info.Title = value;
                    return true;
                case "ARTIST":
                    info.Artist = value;
                    return true;
                case "LEVELDESIGN":
                    info.LevelDesign = value;
                    return true;
                case "BANNER":
                    info.Banner = value;
                    return true;
                case "RANK":
                    if (value.TryParseAsInt(out var rank)) info.Rank = rank;
                    return true;
                case "LNTYPE":
                    if (value.TryParseAsInt(out var lnType)) info.LnType = lnType;
                    return true;
                case "PLAYLEVEL":
                    info.PlayLevel = value;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     Calculates the absolute time (in seconds) for a note at a given tick position,
        ///     accounting for BPM changes and time signature changes.
        ///     Reverse-engineered from the official game's BMS-to-time conversion.
        /// </summary>
        /// <param name="tick">The beat position of the note.</param>
        /// <param name="bpmEntries">All BPM change entries parsed so far, in insertion order.</param>
        /// <param name="timeSigEntries">Time signature changes by measure number.</param>
        /// <returns>Absolute time in seconds.</returns>
        private static float CalculateNoteTime(float tick, List<BpmEntry> bpmEntries,
            Dictionary<int, TimeSigEntry> timeSigEntries)
        {
            // Build a sorted (ascending by tick) list of BPM entries that occur before this note
            // We must sort because BPM entries may have been added in any order from the BMS data
            var relevantBpms = new List<BpmEntry>();
            foreach (var entry in bpmEntries)
            {
                if (entry.Tick < tick)
                    relevantBpms.Add(entry);
            }

            if (relevantBpms.Count == 0) return 0f;
            relevantBpms.Sort();

            var accumulatedTime = 0f;
            var totalBeatOffset = 0f;

            // Walk through BPM segments from earliest to latest
            for (var j = 0; j < relevantBpms.Count; j++)
            {
                var entry = relevantBpms[j];
                var beatFrequency = entry.Freq;

                // Calculate how many beats this segment covers
                float segmentBeats;
                if (j < relevantBpms.Count - 1)
                    segmentBeats = relevantBpms[j + 1].Tick - entry.Tick;
                else
                    segmentBeats = tick - entry.Tick;

                var previousOffset = totalBeatOffset;
                totalBeatOffset += segmentBeats;
                var floorBeat = Mathf.FloorToInt(previousOffset);
                var ceilBeat = Mathf.CeilToInt(totalBeatOffset);

                for (var k = floorBeat; k < ceilBeat; k++)
                {
                    var beatFraction = 1f;

                    if (k == floorBeat)
                        beatFraction = k + 1 - previousOffset;
                    if (k == ceilBeat - 1)
                        beatFraction = totalBeatOffset - (ceilBeat - 1);
                    if (ceilBeat == floorBeat + 1)
                        beatFraction = totalBeatOffset - previousOffset;

                    var timeSigMultiplier = timeSigEntries.TryGetValue(k, out var timeSig)
                        ? timeSig.Percent
                        : 1f;
                    accumulatedTime +=
                        Mathf.RoundToInt(beatFraction * timeSigMultiplier * beatFrequency / 1E-06f) * 1E-06F;
                }
            }

            return accumulatedTime;
        }

        /// <summary>
        ///     Transmutes Bms data into StageInfo data.
        /// </summary>
        /// <param name="bms">The Bms object to transmute.</param>
        /// <returns>The transmuted StageInfo object.</returns>
        internal static StageInfo TransmuteData(Bms bms)
        {
            var stopwatch = Stopwatch.StartNew();

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

            stopwatch.Stop();
            Logger.Msg($"Transmuted BMS in {stopwatch.Elapsed}", false);

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

        private static void LoadMusicData(List<ProcessedNote> noteData)
        {
            short noteId = 1;
            foreach (var note in noteData)
            {
                if (noteId == short.MaxValue)
                {
                    Logger.Warning(
                        $"Cannot process full chart, there are too many objects. Max objects is {short.MaxValue}.");
                    break;
                }

                var configData = note.ToMusicConfigData();
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
            var scene = bms.Info.Genre;
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
            NoteConfigData cachedGearConfig = null;
            var allNoteData = SingletonScriptableObject<NoteDataMananger>.instance.noteDatas;

            for (var i = 0; i < bossData.Count; i++)
            {
                var data = bossData[i];

                if (data.noteData.GetNoteType() != NoteType.Block) continue;

                // Find the next boss animation that is not a gear
                MusicData bossAnimAhead = null;
                for (var j = i + 1; j < bossData.Count; j++)
                {
                    var dataAhead = bossData[j];
                    if (dataAhead.noteData.GetNoteType() == NoteType.Block) continue;

                    bossAnimAhead = dataAhead;
                    break;
                }

                MusicData bossAnimBefore = i > 0 ? bossData[i - 1] : null;

                var aheadTime = bossAnimAhead?.configData.time ?? Decimal.MinValue;
                var beforeTime = bossAnimBefore?.configData.time ?? Decimal.MinValue;

                var diffToAhead = Math.Abs((float)data.configData.time - (float)aheadTime);
                var diffToBefore = Math.Abs((float)data.configData.time - (float)beforeTime);
                var ahead = diffToAhead < diffToBefore;

                var stateBehind = bossAnimBefore != null
                    ? AnimStatesRight[bossAnimBefore.noteData.boss_action]
                    : BossState.OffScreen;
                
                var stateAhead = BossState.OffScreen;
                if (bossAnimAhead != null && AnimStatesLeft.TryGetValue(bossAnimAhead.noteData.boss_action, out var parsedState))
                    stateAhead = parsedState;

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

                // Use cached gear config if it matches, otherwise search for a new one
                if (cachedGearConfig == null
                    || cachedGearConfig.ibms_id != data.noteData.ibms_id
                    || cachedGearConfig.pathway != data.noteData.pathway
                    || cachedGearConfig.scene != data.noteData.scene
                    || cachedGearConfig.speed != data.noteData.speed
                    || !cachedGearConfig.boss_action.StartsWith($"boss_far_atk_{phase}"))
                    foreach (var d in allNoteData)
                    {
                        if (d.ibms_id != data.noteData.ibms_id
                            || d.pathway != data.noteData.pathway
                            || d.scene != data.noteData.scene
                            || d.speed != data.noteData.speed
                            || !d.boss_action.StartsWith($"boss_far_atk_{phase}")) continue;

                        cachedGearConfig = d;
                        break;
                    }

                if (cachedGearConfig == null) continue;
                var phaseGearConfig = cachedGearConfig;

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
            var scene = bms.Info.Genre;
            var sceneIndex = scene.Split('_')[1].ParseAsInt();
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
                            if (prefix is not "00" and not "em" and not "bo")
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
                    var isNoteGemini = Bms.BmsIds[mData.noteData.ibms_id] == Bms.BmsId.Gemini;
                    var isTargetGemini = false;
                    var target = Interop.CreateTypeValue<MusicData>();

                    foreach (var gemini in geminiList.Where(gemini => mData.isAir != gemini.isAir))
                    {
                        target = gemini;
                        isTargetGemini = Bms.BmsIds[gemini.noteData.ibms_id] == Bms.BmsId.Gemini;

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
            if (StartDelayCache.TryGetValue(prefab, out var cached))
                return cached;

            var delay = ResourcesManager.instance.LoadFromName<GameObject>(prefab)
                .GetComponent<SpineActionController>().startDelay;
            StartDelayCache[prefab] = delay;
            return delay;
        }

        private static float GetAnimationDuration(string scene, string animation)
        {
            var cacheKey = (scene, animation);
            if (AnimationDurationCache.TryGetValue(cacheKey, out var cached))
                return cached;

            var controller = ResourcesManager.instance
                .LoadFromName<GameObject>(Boss.Instance.BossFestival($"{scene.Split("_")[1]}01_boss"))
                .GetComponent<SpineActionController>();
            var animations = controller.gameObject.GetComponent<SkeletonAnimation>().skeletonDataAsset
                .GetSkeletonData(true).Animations;

            var animName = animation;
            foreach (var actionData in controller.actionData)
            {
                if (actionData.name != animation) continue;
                if (actionData.actionIdx is { Length: > 0 })
                    animName = actionData.actionIdx[0];
                break;
            }

            var duration = animations
                .Find((Il2CppSystem.Predicate<Animation>)((Animation a) => a.Name == animName)).Duration;
            AnimationDurationCache[cacheKey] = duration;
            return duration;
        }
    }
}