using CustomAlbums.Data;
using CustomAlbums.Utilities;
using System.Text;
using System.Text.Json;
namespace CustomAlbums.Managers
{
    internal class SaveManager
    {
        private const string SaveLocation = "UserData";
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
            try
            {
                File.WriteAllText(Path.Join(SaveLocation, "CustomAlbums.json"), JsonSerializer.Serialize(SaveData));
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to save save file. " + ex.StackTrace);
            }
        }

        internal static void SaveScore(string uid, int score, float accuracy, int maxCombo, string evaluate, int miss)
        {
            var chartSaveData = SaveData.GetChartSaveDataFromUid(uid);
            var customsSave = SaveData;
            // TODO: finish this please :)

        }
    }
}

