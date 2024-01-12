using System.Globalization;
using CustomAlbums.Data;
using CustomAlbums.Utilities;
using System.Text;
using System.Text.Json;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.Nice.Datas;
using Il2CppAssets.Scripts.PeroTools.Nice.Interface;

namespace CustomAlbums.Managers
{
    internal class SaveManager
    {
        private const string SaveLocation = "UserData";
        internal static CustomAlbumsSave SaveData;
        internal static Logger Logger = new(nameof(SaveManager));
        internal static string PreviousScore { get; set; } = "-";

        /// <summary>
        /// Fixes the save file since this version of CAM uses a different naming scheme.
        /// This allows cross-compatibility between CAM 3 and CAM 4, but not from CAM 4 to CAM 3.
        /// </summary>
        internal static void FixSaveFile()
        {
            if (!ModSettings.SavingEnabled) return;
            var firstHistory = SaveData.History.FirstOrDefault();
            var firstHighest = SaveData.Highest.FirstOrDefault();
            var firstFullCombo = SaveData.FullCombo.FirstOrDefault();
            var stringBuilder = new StringBuilder();

            // if we need to fix the history
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
            if (firstHighest.Key.StartsWith("pkg_"))
            {
                var fixedDictionaryHighest = new Dictionary<string, Dictionary<int, CustomChartSave>>(SaveData.Highest.Count);
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

            // If we don't need to fix the FullCombo then return
            if (!firstFullCombo.Key.StartsWith("pkg_")) return;
            
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

        internal static void LoadSaveFile()
        {
            if (!ModSettings.SavingEnabled) return;
            try
            {
                SaveData = Json.Deserialize<CustomAlbumsSave>(File.ReadAllText(Path.Join(SaveLocation, "CustomAlbums.json")));
                FixSaveFile();
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException) SaveData = new CustomAlbumsSave(); 
                else Logger.Warning("Failed to load save file. " + ex.StackTrace);
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
        /// Cleans out all custom data from the vanilla save file since this data should not be stored in the vanilla save.
        /// </summary>
        
        // TODO: Remove this once it's we verify that saving is not occurring after completing a custom chart.
        internal static void SanitizeVanilla()
        {
            var gameCollections = Singleton<DataManager>.instance["Account"]["Collections"];
            var gameHides = Singleton<DataManager>.instance["Account"]["Hides"];
            var gameHistory = Singleton<DataManager>.instance["Account"]["History"];
            var gameHighest = Singleton<DataManager>.instance["Achievement"]["highest"];
            var gameFailCount = Singleton<DataManager>.instance["Achievement"]["fail_count"];
            var gameEasyPass = Singleton<DataManager>.instance["Achievement"]["easy_pass"];
            var gameHardPass = Singleton<DataManager>.instance["Achievement"]["hard_pass"];
            var gameMasterPass = Singleton<DataManager>.instance["Achievement"]["master_pass"];
            var gameFullComboMusic = Singleton<DataManager>.instance["Achievement"]["full_combo_music"];
            var gameUnlockMasters = Singleton<DataManager>.instance["Account"]["UnlockMasters"];

            Logger.Msg("Sanitizing Vanilla save!");
            gameCollections.GetResult<Il2CppSystem.Collections.Generic.List<string>>().RemoveAll((Il2CppSystem.Predicate<string>)(uid => uid.StartsWith($"{AlbumManager.Uid}-")));
            gameHides.GetResult<Il2CppSystem.Collections.Generic.List<string>>().RemoveAll((Il2CppSystem.Predicate<string>)(uid => uid.StartsWith($"{AlbumManager.Uid}-")));
            gameHistory.GetResult<Il2CppSystem.Collections.Generic.List<string>>().RemoveAll((Il2CppSystem.Predicate<string>)(uid => uid.StartsWith($"{AlbumManager.Uid}-")));
            gameHighest.GetResult<Il2CppSystem.Collections.Generic.List<IData>>().RemoveAll((Il2CppSystem.Predicate<IData>)(data => data["uid"].GetResult<string>().StartsWith($"{AlbumManager.Uid}-")));
            gameFailCount.GetResult<Il2CppSystem.Collections.Generic.List<IData>>().RemoveAll((Il2CppSystem.Predicate<IData>)(data => data["uid"].GetResult<string>().StartsWith($"{AlbumManager.Uid}-")));
            gameEasyPass.GetResult<Il2CppSystem.Collections.Generic.List<string>>().RemoveAll((Il2CppSystem.Predicate<string>)(uid => uid.StartsWith($"{AlbumManager.Uid}-")));
            gameHardPass.GetResult<Il2CppSystem.Collections.Generic.List<string>>().RemoveAll((Il2CppSystem.Predicate<string>)(uid => uid.StartsWith($"{AlbumManager.Uid}-")));
            gameMasterPass.GetResult<Il2CppSystem.Collections.Generic.List<string>>().RemoveAll((Il2CppSystem.Predicate<string>)(uid => uid.StartsWith($"{AlbumManager.Uid}-")));
            gameFullComboMusic.GetResult<Il2CppSystem.Collections.Generic.List<string>>().RemoveAll((Il2CppSystem.Predicate<string>)(uid => uid.StartsWith($"{AlbumManager.Uid}-")));
            gameUnlockMasters.GetResult<Il2CppSystem.Collections.Generic.List<string>>().RemoveAll((Il2CppSystem.Predicate<string>)(uid => uid.StartsWith($"{AlbumManager.Uid}-")));
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
        internal static void SaveScore(string uid, int musicDifficulty, int score, float accuracy, int maxCombo, string evaluate, int miss)
        {
            if (!ModSettings.SavingEnabled) return;

            var album = AlbumManager.GetByUid(uid);
            if (album is null) return;

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

            var albumName = $"album_{Path.GetFileNameWithoutExtension(album.Path)}";

            // Create new album save 
            if (!SaveData.Highest.ContainsKey(albumName)) 
                SaveData.Highest.Add(albumName, new Dictionary<int, CustomChartSave>());

            var currChartScore = SaveData.Highest[albumName];
            
            // Create new save data if the difficulty doesn't exist
            if (!currChartScore.ContainsKey(musicDifficulty)) 
                currChartScore.Add(musicDifficulty, new CustomChartSave());

            // Set previous score for PnlVictory logic
            var newScore = currChartScore[musicDifficulty];
            PreviousScore = newScore.Passed ? newScore.Score.ToString() : "-";

            // Set the correct new score, taking the max of everything
            newScore.Passed = true;
            newScore.Accuracy = Math.Max(accuracy, newScore.Accuracy);
            newScore.Score = Math.Max(score, newScore.Score);
            newScore.Combo = Math.Max(maxCombo, newScore.Combo);
            newScore.Evaluate = Math.Max(newEvaluate, newScore.Evaluate);
            newScore.AccuracyStr = string.Create(CultureInfo.InvariantCulture, $"{newScore.Accuracy / 100:P2}");
            newScore.Clear++;

            if (miss != 0) return;
            
            // If there were no misses then add the chart/difficulty to the FullCombo list
            if (!SaveData.FullCombo.ContainsKey(albumName)) 
                SaveData.FullCombo.Add(albumName, new List<int>());

            if (!SaveData.FullCombo[albumName].Contains(musicDifficulty)) 
                SaveData.FullCombo[albumName].Add(musicDifficulty);

            SaveSaveFile();
        }
    }
}

