using System.Text;
using System.Text.Json;
using CustomAlbums.Data;
using CustomAlbums.Utilities;
using System.IO.Compression;
using CustomAlbums.Patches;

namespace CustomAlbums.Managers
{
    internal class SaveManager
    {
        private const string SaveLocation = "UserData";
        internal static CustomAlbumsSave SaveData;
        internal static Logger Logger = new(nameof(SaveManager));
        internal static string PreviousScore { get; set; } = "-";

        /// <summary>
        ///     Fixes the save file since this version of CAM uses a different naming scheme.
        ///     This allows cross-compatibility between CAM 3 and CAM 4, but not from CAM 4 to CAM 3.
        /// </summary>
        internal static void FixSaveFile()
        {
            if (!ModSettings.SavingEnabled) return;
            var firstHistory = SaveData.History.FirstOrDefault();
            var firstHighest = SaveData.Highest.FirstOrDefault();
            var firstFullCombo = SaveData.FullCombo.FirstOrDefault();
            var stringBuilder = new StringBuilder();

            // If we need to fix the history
            if (firstHistory != null && firstHistory.StartsWith("pkg_"))
            {
                var fixedQueue = new Queue<string>(SaveData.History.Count);
                foreach (var history in SaveData.History.Where(history => history.StartsWith("pkg_")))
                {
                    stringBuilder.Clear();
                    stringBuilder.Append(history);
                    stringBuilder.Remove(0, 4);
                    stringBuilder.Insert(0, "album_");
                    fixedQueue.Enqueue(stringBuilder.ToString());
                }

                SaveData.History = fixedQueue;
            }

            // If we need to fix the highest
            if (!firstHighest.Equals(default(KeyValuePair<string, Dictionary<int, CustomChartSave>>)) &&
                firstHighest.Key.StartsWith("pkg_"))
            {
                var fixedDictionaryHighest =
                    new Dictionary<string, Dictionary<int, CustomChartSave>>(SaveData.Highest.Count);
                foreach (var (key, value) in SaveData.Highest.Where(kv => kv.Key.StartsWith("pkg_")))
                {
                    stringBuilder.Clear();
                    stringBuilder.Append(key);
                    stringBuilder.Remove(0, 4);
                    stringBuilder.Insert(0, "album_");
                    fixedDictionaryHighest.Add(stringBuilder.ToString(), value);
                }

                SaveData.Highest = fixedDictionaryHighest;
            }

            if (!SaveData.UnlockedMasters.Any()) 
            {
                var unlockedHighest = SaveData.Highest.Where(kv =>
                    kv.Value.ContainsKey(3) && kv.Value.TryGetValue(2, out var chartSave) && chartSave.Evaluate >= 4).Select(kv => kv.Key);
                var folderCharts = AlbumManager.LoadedAlbums.Where(kv => kv.Value.HasDifficulty(2) && kv.Value.HasDifficulty(3) && !kv.Value.IsPackaged).Select(kv => kv.Key);
                var concat = unlockedHighest.Concat(folderCharts);
                SaveData.UnlockedMasters.UnionWith(concat);
            }

            // If we don't need to fix the FullCombo then return
            if (!firstFullCombo.Equals(default(KeyValuePair<string, List<int>>)) &&
                !firstFullCombo.Key.StartsWith("pkg_")) return;

            var fixedDictionaryFc = new Dictionary<string, List<int>>(SaveData.FullCombo.Count);
            foreach (var (key, value) in SaveData.FullCombo.Where(kv => kv.Key.StartsWith("pkg_")))
            {
                if (!key.StartsWith("pkg_")) continue;
                stringBuilder.Clear();
                stringBuilder.Append(key);
                stringBuilder.Remove(0, 4);
                stringBuilder.Insert(0, "album_");
                fixedDictionaryFc.Add(stringBuilder.ToString(), value);
            }

            SaveData.FullCombo = fixedDictionaryFc;
        }

        private static void RestoreBackup()
        {
            var backupPath = Path.Join(SaveLocation, "Backups", "Backups.zip");
            
            // If a backup does not exist at all
            if (!File.Exists(backupPath))
            {
                Logger.Fail("No backups found. Please delete the CustomAlbums.json file in UserData folder to create a new save.");
                return;
            }

            // Traverse the .zip file, trying every single backup in this archive
            using var backupEntries = ZipFile.OpenRead(backupPath);
            foreach (var backup in backupEntries.Entries.OrderByDescending(bak => bak.LastWriteTime)
                         .Where(bak => bak.Name.EndsWith("CustomAlbums.json.bak")))
            {
                try
                {
                    SaveData = Json.Deserialize<CustomAlbumsSave>(backup.Open());
                }
                catch (Exception)
                {
                    continue;
                }
                
                // If our backup that we are trying to load is valid but empty, continue to try and find the last save with data in it
                if (SaveData.IsEmpty()) continue;
                Logger.Success($"Restored backup from {backup.LastWriteTime.DateTime}.");
                return;
            }
            Logger.Fail("Could not restore save file. Please delete the CustomAlbums.json file in UserData folder to create a new save.");
        }

        internal static void LoadSaveFile()
        {
            if (!ModSettings.SavingEnabled) return;
            try
            {
                using var fileStream = File.OpenRead(Path.Join(SaveLocation, "CustomAlbums.json"));
                SaveData = Json.Deserialize<CustomAlbumsSave>(fileStream);
                FixSaveFile();
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException)
                {
                    SaveData = new CustomAlbumsSave();
                }
                else
                {
                    Logger.Warning("Could not load save file. Attempting to restore backup...");
                    RestoreBackup();
                }
            }
        }

        internal static void SaveSaveFile()
        {
            if (!ModSettings.SavingEnabled) return;
            try
            {
                if (SaveData is null)
                {
                    Logger.Warning("Trying to save null data, not saving.");
                    return;
                }

                File.WriteAllText(Path.Join(SaveLocation, "CustomAlbums.json"), JsonSerializer.Serialize(SaveData));
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to save save file. " + ex.StackTrace);
            }
        }

        /// <summary>
        /// Saves custom score given scoring information.
        /// </summary>
        /// <param name="uid">The UID of the chart.</param>
        /// <param name="musicDifficulty">The difficulty index of the chart played.</param>
        /// <param name="score">The score of the play.</param>
        /// <param name="accuracy">The accuracy of the play.</param>
        /// <param name="maxCombo">The maximum combo of the play.</param>
        /// <param name="evaluate">The judgement ranking of the play.</param>
        /// <param name="miss">The amount of misses in the play.</param>
        internal static void SaveScore(string uid, int musicDifficulty, int score, float accuracy, int maxCombo,
            string evaluate, int miss)
        {
            if (!ModSettings.SavingEnabled) return;

            var album = AlbumManager.GetByUid(uid);
            if (!album?.IsPackaged ?? true) return;

            var newEvaluate = evaluate switch
            {
                "sss" => 6,
                "ss" => 5,
                "s" => 4,
                "a" => 3,
                "b" => 2,
                "c" => 1,
                _ => 0
            };

            var albumName = album.AlbumName;

            // Create new album save 
            SaveData.Highest.TryAdd(albumName, new Dictionary<int, CustomChartSave>());

            var currChartScore = SaveData.Highest[albumName];

            // Create new save data if the difficulty doesn't exist
            currChartScore.TryAdd(musicDifficulty, new CustomChartSave());

            // Set previous score for PnlVictory logic
            var newScore = currChartScore[musicDifficulty];
            PreviousScore = newScore.Passed ? newScore.Score.ToString() : "-";

            // Set the correct new score, taking the max of everything
            newScore.Passed = true;
            newScore.Accuracy = Math.Max(accuracy, newScore.Accuracy);
            newScore.Score = Math.Max(score, newScore.Score);
            newScore.Combo = Math.Max(maxCombo, newScore.Combo);
            newScore.Evaluate = Math.Max(newEvaluate, newScore.Evaluate);
            newScore.AccuracyStr = (newScore.Accuracy / 100).ToStringInvariant("P2");
            newScore.Clear++;

            if (musicDifficulty is 2 && AlbumManager.LoadedAlbums[albumName].HasDifficulty(3) && newScore.Evaluate >= 4)
                SaveData.UnlockedMasters.Add(albumName);

            // Update the IData for the played chart
            var dataIndex = DataInjectPatch.DataList.GetIndexByUid(album.Uid, musicDifficulty);
            if (dataIndex != -1)
            {
                DataInjectPatch.DataList.RemoveAt(dataIndex);
            }

            var newIData = DataInjectPatch.CreateIData(album, musicDifficulty, newScore);
            DataInjectPatch.DataList.Add(newIData);

            // If there were no misses then add the chart/difficulty to the FullCombo list
            if (miss != 0) return;

            SaveData.FullCombo.TryAdd(albumName, new List<int>());

            if (!SaveData.FullCombo[albumName].Contains(musicDifficulty))
                SaveData.FullCombo[albumName].Add(musicDifficulty);
        }
    }
}