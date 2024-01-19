using System.IO.Compression;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using CustomAlbums.Managers;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.Nice.Datas;
using Il2CppAssets.Scripts.PeroTools.Nice.Interface;
using System.Text.Json;
using System.Text.Json.Nodes;
using CustomAlbums.Data;

namespace CustomAlbums.Utilities
{
    internal class Backup
    {
        private static readonly Logger Logger = new(nameof(Backup));
        private static string BackupPath => Path.Combine(Directory.GetCurrentDirectory(), "UserData/Backups");
        private static string BackupVanilla => Path.Combine(BackupPath, "Vanilla.sav.bak");
        private static string BackupVanillaDebug => Path.Combine(BackupPath, "VanillaDebug.json");
        private static string BackupCustom => Path.Combine(BackupPath, "CustomAlbums.json.bak");
        private static string BackupZip => Path.Combine(BackupPath, "Backups.zip");
        private static TimeSpan MaxBackupTime => TimeSpan.FromDays(30);

        internal static void InitBackups()
        {
            Directory.CreateDirectory(BackupPath);

            CompressBackups();
            CreateBackup(BackupVanilla, Singleton<DataManager>.instance.ToBytes());
            CreateBackup(BackupVanillaDebug, JsonSerializer.Serialize(ToJsonDict(Singleton<DataManager>.instance.datas)));
            CreateBackup(BackupCustom, SaveManager.SaveData);
            ClearOldBackups();
        }
        private static void CreateBackup(string filePath, object data)
        {
            try
            {
                if (data is null)
                {
                    Logger.Warning("Could not create backup of null data!");
                    return;
                }
                var wroteFile = false;
                
                switch (data)
                {
                    case string str:
                        File.WriteAllText(filePath, str);
                        wroteFile = true;
                        break;

                    case byte[] bytes:
                        File.WriteAllBytes(filePath, bytes);
                        wroteFile = true;
                        break; 
                        
                    case Il2CppStructArray<byte> ilBytes:
                        File.WriteAllBytes(filePath, ilBytes);
                        wroteFile = true;
                        break;

                    case CustomAlbumsSave save:
                        File.WriteAllText(filePath, JsonSerializer.Serialize(save));
                        wroteFile = true;
                        break;

                    default:
                        Logger.Warning("Could not create backup for unsupported data type " + data.GetType().FullName);
                        break;
                }

                if (wroteFile) Logger.Msg($"Saved backup: {filePath}");
            }
            catch (Exception e)
            {
                Logger.Error("Backup failed: " + e);
            }
        }

        private static void ClearOldBackups()
        {
            try
            {
                var backups = Directory.EnumerateFiles(BackupPath).ToList();
                foreach (var backup in from backup in backups let backupDate = Directory.GetLastWriteTime(backup) where (DateTime.Now - backupDate).Duration() > MaxBackupTime.Duration() select backup)
                {
                    Logger.Msg("Removing old backup: " + backup);
                    File.Delete(backup);
                }

                if (!File.Exists(BackupZip)) return;
                
                using var zip = ZipFile.OpenRead(BackupZip);
                foreach (var entry in zip.Entries.ToList().Where(entry => (DateTime.Now - entry.LastWriteTime).Duration() > MaxBackupTime.Duration()))
                {
                    Logger.Msg("Removing compressed old backup: " + entry.Name);
                    zip.GetEntry(entry.Name)?.Delete();
                }
            }
            catch (Exception e)
            {
                Logger.Error("Clearing old backups failed: " + e);
            }
        }

        private static void CompressBackups()
        {
            try
            {
                using var zip = ZipFile.Open(BackupZip, ZipArchiveMode.Update);

                var filesList = Directory.EnumerateFiles(BackupPath).Where(name => Path.GetExtension(name) != ".zip").ToList();
                if (filesList.Any())
                {
                    filesList.ForEach(entry =>
                    {
                        var newFileName = Directory.GetLastWriteTime(entry).ToString("yyyy_MM_dd_H_mm_ss-") + Path.GetFileName(Path.GetFileName(entry));
                        zip.CreateEntryFromFile(entry, newFileName);
                        File.Delete(entry);
                    });
                }
            }
            catch (Exception e)
            {
                Logger.Error("Compressing previous backups failed: " + e);
            }
        }
        private static Dictionary<string, JsonObject> ToJsonDict(Il2CppSystem.Collections.Generic.Dictionary<string, IData> dataList)
        {
            var dictionary = new Dictionary<string, JsonObject>();
            foreach (var keyValuePair in dataList)
            {
                var singletonDataObject = keyValuePair.Value?.TryCast<SingletonDataObject>();
                if (singletonDataObject != null)
                    dictionary.Add(keyValuePair.Key, Json.Deserialize<JsonObject>(singletonDataObject.ToJson()));
                
            }
            return dictionary;
        }
    }
}
