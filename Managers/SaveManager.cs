using CustomAlbums.Data;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using System.Text;
using System.Text.Json;

namespace CustomAlbums.Managers
{
    internal class SaveManager
    {
        private const string SAVE_LOCATION = "UserData";
        internal static CustomAlbumsSave SaveData;
        internal static Logger Logger = new(nameof(SaveManager));

        /// <summary>
        /// Fixes the save file since this version of CAM uses a different naming scheme.
        /// This allows cross-compatibility between CAM 3 and CAM 4, but not from CAM 4 to CAM 3.
        /// </summary>
        internal static void FixSaveFile()
        {
            var firstHistory = SaveData.History.FirstOrDefault();
            var firstHighest = SaveData.Highest.FirstOrDefault();
            var firstFullCombo = SaveData.FullCombo.FirstOrDefault();

            // if we need to fix the history
            if (firstHistory.StartsWith("pkg_"))
            {
                var fixedQueue = new Queue<string>(SaveData.History.Count);
                var stringBuilder = new StringBuilder();
                foreach (var history in SaveData.History)
                {
                    if (history.StartsWith("pkg_"))
                    {
                        stringBuilder.Clear();
                        stringBuilder.Append(history);
                        stringBuilder.Remove(0, 4);
                        stringBuilder.Insert(0, "album_");
                        fixedQueue.Enqueue(stringBuilder.ToString());
                    }
                }
                SaveData.History = fixedQueue;
            }
            
            // if we need to fix the highest
            if (firstHighest.Key.StartsWith("pkg_"))
            {
                var fixedDictionary = new Dictionary<string, Dictionary<int, CustomChartSave>>(SaveData.Highest.Count);
                var stringBuilder = new StringBuilder();
                foreach (var (key, value) in SaveData.Highest)
                {
                    if (key.StartsWith("pkg_"))
                    {
                        stringBuilder.Clear();
                        stringBuilder.Append(key);
                        stringBuilder.Remove(0, 4);
                        stringBuilder.Insert(0, "album_");
                        fixedDictionary.Add(stringBuilder.ToString(), value);
                    }
                }
                SaveData.Highest = fixedDictionary;
            }

            // if we need to fix the fullcombo
            if (firstFullCombo.Key.StartsWith("pkg_"))
            {
                var fixedDictionary = new Dictionary<string, List<int>>(SaveData.FullCombo.Count);
                var stringBuilder = new StringBuilder();
                foreach (var (key, value) in SaveData.FullCombo)
                {
                    if (key.StartsWith("pkg_"))
                    {
                        stringBuilder.Clear();
                        stringBuilder.Append(key);
                        stringBuilder.Remove(0, 4);
                        stringBuilder.Insert(0, "album_");
                        fixedDictionary.Add(stringBuilder.ToString(), value);
                    }
                }
                SaveData.FullCombo = fixedDictionary;
            }
        }

        internal static void LoadSaveFile()
        {
            try
            {
                SaveData = Json.Deserialize<CustomAlbumsSave>(File.ReadAllText(Path.Join(SAVE_LOCATION, "CustomAlbums.json")));
                FixSaveFile();
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException) SaveData = new(); 
                else Logger.Warning("Failed to load save file. " + ex.StackTrace);
            }
        }
        internal static void SaveSaveFile()
        {
            try
            {
                File.WriteAllText(Path.Join(SAVE_LOCATION, "CustomAlbums.json"), JsonSerializer.Serialize(SaveData));
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to save save file. " + ex.StackTrace);
            }
        }
    }
}

