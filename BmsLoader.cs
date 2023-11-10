using System.Text.Json.Nodes;
using CustomAlbums.Data;
using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using Il2CppAssets.Scripts.GameCore;
using UnityEngine;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums
{
    internal static class BmsLoader
    {
        private static readonly Logger Logger = new(nameof(BmsLoader));
        private static int Delay = 0;

        /// <summary>
        /// Creates a Bms object from a BMS file.
        /// </summary>
        /// <param name="stream">MemoryStream of BMS file.</param>
        /// <param name="bmsName">Name of BMS score.</param>
        /// <returns>Loaded Bms object.</returns>
        internal static Bms Load(MemoryStream stream, string bmsName)
        {
            Logger.Msg($"Loading bms {bmsName}...");

            var bpmDict = new Dictionary<string, float>();
            var notePercents = new Dictionary<int, JsonObject>();
            var dataList = new List<JsonObject>();
            var notesArray = new JsonArray();
            var info = new JsonObject();

            var streamReader = new StreamReader(stream);
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
                    bpmDict.Add(bpmKey, float.Parse(value));

                    if (bpmKey != "00") continue;

                    var freq = 60f / float.Parse(value) * 4f;
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

                    var beat = int.Parse(key[..3]);
                    var typeCode = key.Substring(3, 2);
                    var type = Bms.Channels[typeCode];

                    if (type == Bms.ChannelType.SpTimesig)
                    {
                        var obj = new JsonObject
                        {
                            { "beat", beat },
                            { "percent", float.Parse(value) }
                        };
                        notePercents.Add(beat, obj);
                    }
                    else
                    {
                        var objLength = value.Length / 2;
                        for (var i = 0; i < objLength; i++)
                        {
                            var note = value.Substring(i * 2, 2);
                            if (note == "00") continue;

                            var tick = (float)i / objLength + beat;

                            if (type is Bms.ChannelType.SpBpmDirect or Bms.ChannelType.SpBpmLookup)
                            {
                                // Handle BPM changes
                                var freqDivide = type == Bms.ChannelType.SpBpmLookup || bpmDict.ContainsKey(note)
                                    ? bpmDict[note]
                                    : Convert.ToInt32(note, 16);
                                var freq = 60f / (freqDivide * 4f);

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

                                    return tickL.CompareTo(tickR);
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
                                        else if (k == ceilOffset - 1)
                                            off = totalOffset - (ceilOffset - 1);
                                        else if (ceilOffset == floorOffset + 1)
                                            off = totalOffset - localOffset;

                                        notePercents.TryGetValue(k, out var node);
                                        var percent = node?["percent"].GetValue<float>() ?? 1f;
                                        time += Mathf.RoundToInt(off * percent * freq / 1E-06f) * 1E-06F;
                                    }
                                }

                                //Logger.Msg("Time when setting obj: " + time);
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
        /// Transmutes Bms data into StageInfo data.
        /// </summary>
        /// <param name="bms">The Bms object to transmute.</param>
        /// <returns>The transmuted StageInfo object.</returns>
        internal static StageInfo TransmuteData(Bms bms)
        {
            if (Bms.NoteData.Count == 0) Bms.InitNoteData();
            MusicDataManager.Clear();
            Delay = 0;

            var stageInfo = ScriptableObject.CreateInstance<StageInfo>();

            return stageInfo;
        }
    }
}