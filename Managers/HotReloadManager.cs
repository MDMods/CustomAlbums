using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using CustomAlbums.Data;
using CustomAlbums.Patches;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.Managers;
using Il2CppAssets.Scripts.UI.Panels;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppPeroTools2.Resources;
using UnityEngine;
using Logger = CustomAlbums.Utilities.Logger;
using Object = UnityEngine.Object;

namespace CustomAlbums.Managers;

internal static class HotReloadManager
{
    private static readonly Logger Logger = new(nameof(HotReloadManager));

    private static readonly Dictionary<string, MusicInfo> HotLoadedMusicInfos = new();
    private static readonly Dictionary<string, string> HotLoadedMusicNames = new();
    private static readonly Dictionary<string, string> HotLoadedMusicAuthors = new();

    private static float _lastProcessTime;

    private static ConcurrentQueue<string> AlbumsToAdd { get; } = new();

    private static ConcurrentQueue<string> AlbumsToDelete { get; } = new();
    private static ConcurrentDictionary<string, DateTime> LastFileEvent { get; } = new();
    internal static PnlStage PnlStageInstance { get; set; }

    private static bool IsFileUnlocked(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                var infoPath = Path.Combine(path, "info.json");
                if (!File.Exists(infoPath)) return false;
                
                using var fileStream = File.Open(infoPath, FileMode.Open, FileAccess.Read, FileShare.None);
                return fileStream.Length > 0;
            }
            else
            {
                if (!File.Exists(path)) return false;
                using var fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
                return fileStream.Length > 0;
            }
        }
        catch (Exception ex) when (ex is FileNotFoundException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }


    private static string GetTopLevelName(string relativePath)
    {
        var separatorIndex = relativePath.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
        return separatorIndex >= 0 ? relativePath.Substring(0, separatorIndex) : relativePath;
    }



    private static MusicExInfo CreateMusicExInfo(Album album)
    {
        var changedDiff = new Il2CppStructArray<int>(5)
        {
            [0] = 1, // Easy
            [1] = 2, // Hard
            [2] = 3, // Master
            [3] = 4, // Supreme
            [4] = 5 // Touhou thing
        };

        var musicExInfo = new MusicExInfo
        {
            m_AlbumIndex = AlbumManager.Uid + 1,
            m_AlbumUidIndex = AlbumManager.Uid,
            m_MusicIndex = album.Index,
            m_AlbumUidName = $"music_package_{AlbumManager.Uid}",
            m_AlbumJsonName = AlbumManager.JsonName,
            m_ChangedDiff = changedDiff
        };
        return musicExInfo;
    }

    private static int ProcessAdditions()
    {
        var addedCount = 0;

        if (AlbumsToAdd.TryDequeue(out var path))
            try
            {
                var album = AlbumManager.LoadOne(path);
                if (album == null) Logger.Warning($"Failed to load album from {path}");

                var albumName = album.AlbumName;
                var uid = $"{AlbumManager.Uid}-{album.Index}";
                var albumInfo = album.Info;
                Logger.Msg($"Adding {albumName} (UID: {uid})");

                var masterAlbums = Singleton<ConfigManager>.instance.GetConfigObject<DBConfigAlbums>();
                var albumsInfo = masterAlbums?.GetAlbumsInfoByUid(AlbumManager.MusicPackage);
                var globalAlbumConfig = albumsInfo != null
                    ? Singleton<ConfigManager>.instance.GetConfigObject<DBConfigALBUM>(albumsInfo.albumJsonIndex)
                    : null;

                if (globalAlbumConfig != null)
                {
                    var jsonArray = new JsonArray();
                    var localJsonArray = new JsonArray();

                    foreach (var (albumStr, albumObj) in AlbumManager.LoadedAlbums)
                    {
                        var aInfo = albumObj.Info;
                        var displayName = aInfo.Name ?? "";

                        var customChartJson = new
                        {
                            uid = albumObj.Uid,
                            name = displayName,
                            author = aInfo.Author ?? "",
                            bpm = aInfo.Bpm ?? "0",
                            music = $"{albumStr}_music",
                            demo = $"{albumStr}_demo",
                            cover = $"{albumStr}_cover",
                            noteJson = $"{albumStr}_map",
                            scene = aInfo.Scene ?? "scene_01",
                            unlockLevel = "0",
                            levelDesigner = aInfo.LevelDesigner ?? "",
                            levelDesigner1 = aInfo.LevelDesigner1 ?? aInfo.LevelDesigner ?? "",
                            levelDesigner2 = aInfo.LevelDesigner2 ?? aInfo.LevelDesigner ?? "",
                            levelDesigner3 = aInfo.LevelDesigner3 ?? aInfo.LevelDesigner ?? "",
                            levelDesigner4 = aInfo.LevelDesigner4 ?? aInfo.LevelDesigner ?? "",
                            levelDesigner5 = aInfo.LevelDesigner5 ?? aInfo.LevelDesigner ?? "",
                            difficulty1 = aInfo.Difficulty1 ?? "0",
                            difficulty2 = aInfo.Difficulty2 ?? "0",
                            difficulty3 = aInfo.Difficulty3 ?? "0",
                            difficulty4 = aInfo.Difficulty4 ?? "0",
                            difficulty5 = aInfo.Difficulty5 ?? "0"
                        };
                        jsonArray.Add(JsonSerializer.SerializeToNode(customChartJson));

                        localJsonArray.Add(JsonSerializer.SerializeToNode(new
                        {
                            name = displayName,
                            author = aInfo.Author ?? ""
                        }));
                    }

                    var fullJsonStr = JsonSerializer.Serialize(jsonArray);
                    var fullLocalJsonStr = JsonSerializer.Serialize(localJsonArray);

                    globalAlbumConfig.Deserialize(fullJsonStr);

                    var localDicProp =
                        typeof(BaseDBConfigLocalObject<DBConfigLocalALBUM, LocalALBUMInfo>).GetProperty("m_LocalDic",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (localDicProp != null)
                    {
                        var globalLocalDic =
                            localDicProp.GetValue(globalAlbumConfig) as
                                Il2CppSystem.Collections.Generic.Dictionary<int, DBConfigLocalALBUM>;
                        if (globalLocalDic != null)
                        {
                            globalLocalDic.Clear();
                            for (var i = 0; i <= 15; i++)
                            {
                                var newLocalAlbum = new DBConfigLocalALBUM();
                                newLocalAlbum.Deserialize(fullLocalJsonStr);
                                globalLocalDic.Add(i, newLocalAlbum);
                            }
                        }
                    }

                    GlobalDataBase.s_DbMusicTag.AddAllMusicInfo(globalAlbumConfig);

                    var newMusicInfoList = new Il2CppSystem.Collections.Generic.List<MusicInfo>();
                    globalAlbumConfig.GetAllMusicInfo(newMusicInfoList);
                    var idx = 0;
                    foreach (var m in newMusicInfoList)
                    {
                        var uidSplit = m.uid.Split('-');
                        if (uidSplit.Length < 2)
                        {
                            idx++;
                            continue;
                        }

                        if (!int.TryParse(uidSplit[1], out var parsedIndex))
                        {
                            idx++;
                            continue;
                        }

                        var aObj = AlbumManager.LoadedAlbums.Values.FirstOrDefault(a => a.Index == parsedIndex);
                        if (aObj != null)
                        {
                            m.Init(idx);
                            m.InitExInfo();
                            m.m_MusicExInfo = CreateMusicExInfo(aObj);

                            HotLoadedMusicInfos[m.uid] = m;
                            HotLoadedMusicNames[m.uid] = aObj.Info.Name ?? "";
                            HotLoadedMusicAuthors[m.uid] = aObj.Info.Author ?? "";
                        }

                        idx++;
                    }

                    Logger.Msg($"Transmuted MusicInfo for {uid}");
                }
                else
                {
                    Logger.Error("globalAlbumConfig is null! Cannot inject metadata.");
                }

                try
                {
                    var dhColBase = DataHelper.collections;
                    var dhColPtr = dhColBase != null ? dhColBase.Pointer : IntPtr.Zero;
                    var uids = GlobalDataBase.s_DbMusicTag.m_StageShowMusicUids;
                    if (uids != null)
                        if (!uids.Contains(uid))
                            if (dhColPtr == IntPtr.Zero || uids.Pointer != dhColPtr)
                            {
                                uids.Add(uid);
                                Logger.Msg($"Added {uid} to m_StageShowMusicUids");
                            }

                    var allMusicTag = GlobalDataBase.dbMusicTag.GetAlbumTagInfo(0);
                    if (allMusicTag != null)
                    {
                        if (allMusicTag.m_MusicUids != null && !allMusicTag.m_MusicUids.Contains(uid))
                            if (allMusicTag.m_MusicUids.Pointer != dhColPtr)
                                allMusicTag.m_MusicUids.Add(uid);
                        if (allMusicTag.m_DisplayMusicUids != null)
                            foreach (var d in allMusicTag.m_DisplayMusicUids)
                                if (d.musicUids != null && !d.musicUids.Contains(uid))
                                    if (d.musicUids.Pointer != dhColPtr)
                                        d.musicUids.Add(uid);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to add to view lists: {ex.Message}");
                }

                // Inject search tags into ConfigManager
                try
                {
                    var config = Singleton<ConfigManager>.instance.GetConfigObject<DBConfigMusicSearchTag>();
                    var searchTag = new MusicSearchTagInfo
                    {
                        uid = uid,
                        listIndex = config.count
                    };

                    var tags = new List<string> { "custom albums" };
                    if (albumInfo.SearchTags != null) tags.AddRange(albumInfo.SearchTags);
                    if (!string.IsNullOrEmpty(albumInfo.NameRomanized)) tags.Add(albumInfo.NameRomanized);
                    for (var i = 0; i < tags.Count; i++) tags[i] = tags[i].ToLower();
                    searchTag.tag = new Il2CppStringArray(tags.ToArray());

                    if (!config.m_Dictionary.ContainsKey(uid))
                    {
                        config.m_Dictionary.Add(uid, searchTag);
                        config.list.Add(searchTag);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to add search tags: {ex.Message}");
                }

                // Preload cover resource
                try
                {
                    if (album.HasFile("cover.png"))
                        ResourcesManager.instance
                            .LoadFromName<Sprite>($"{albumName}_cover")
                            .hideFlags |= HideFlags.DontUnloadUnusedAsset;
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to preload cover: {ex.Message}");
                }

                try
                {
                    HiddenSupportPatch.AddHidden(album);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to add hidden support: {ex.Message}");
                }

                addedCount++;
                Logger.Msg($"Successfully added {albumInfo.Name}");
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error adding album: {ex.Message}");
                Logger.Warning(ex.StackTrace);
            }

        return addedCount;
    }

    private static int ProcessDeletions()
    {
        var deletedCount = 0;

        if (AlbumsToDelete.TryDequeue(out var albumKey))
            try
            {
                Logger.Msg($"Removing {albumKey}");

                if (!AlbumManager.LoadedAlbums.TryGetValue(albumKey, out var album))
                    Logger.Warning($"Album {albumKey} not found");

                var uid = $"{AlbumManager.Uid}-{album.Index}";

                HotLoadedMusicInfos.Remove(uid);
                HotLoadedMusicNames.Remove(uid);
                HotLoadedMusicAuthors.Remove(uid);

                try
                {
                    HiddenSupportPatch.RemoveHidden(uid);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to remove hidden support: {ex.Message}");
                }

                // Remove from game music database
                try
                {
                    var musicInfo = GlobalDataBase.s_DbMusicTag.GetMusicInfoFromAll(uid);
                    if (musicInfo != null) GlobalDataBase.s_DbMusicTag.RemoveShowMusicUid(musicInfo);

                    var allMusicInfo = GlobalDataBase.s_DbMusicTag.m_AllMusicInfo;
                    if (allMusicInfo != null && allMusicInfo.ContainsKey(uid)) allMusicInfo.Remove(uid);

                    if (GlobalDataBase.s_DbMusicTag.m_AllAlbumTagData != null)
                        foreach (var tag in GlobalDataBase.s_DbMusicTag.m_AllAlbumTagData)
                        {
                            var tagInfo = tag.Value;
                            if (tagInfo.m_MusicUids != null && tagInfo.m_MusicUids.Contains(uid))
                                tagInfo.m_MusicUids.Remove(uid);
                            if (tagInfo.m_DisplayMusicUids != null)
                                foreach (var d in tagInfo.m_DisplayMusicUids)
                                    if (d.musicUids != null && d.musicUids.Contains(uid))
                                        d.musicUids.Remove(uid);
                        }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to remove from database: {ex.Message}");
                }

                // Remove from ConfigManager
                try
                {
                    var config = Singleton<ConfigManager>.instance.GetConfigObject<DBConfigMusicSearchTag>();
                    if (config != null)
                        if (config.m_Dictionary.ContainsKey(uid))
                        {
                            var tagInfo = config.m_Dictionary[uid];
                            config.m_Dictionary.Remove(uid);
                            config.list.Remove(tagInfo);
                        }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to remove search tags: {ex.Message}");
                }

                // Clear resource cache
                AlbumManager.LoadedAlbums.Remove(albumKey);
                AssetPatch.RemoveFromCache($"{albumKey}_demo");
                AssetPatch.RemoveFromCache($"{albumKey}_music");
                AssetPatch.RemoveFromCache($"{albumKey}_cover");

                deletedCount++;
                Logger.Msg($"Removed {albumKey}");
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error removing: {ex.Message}");
                Logger.Warning(ex.StackTrace);
            }

        return deletedCount;
    }

    private static void RebuildCustomAlbumsTag()
    {
        try
        {
            var existingTag = GlobalDataBase.dbMusicTag.GetAlbumTagInfo(AlbumManager.Uid);
            if (existingTag != null)
            {
                var uids = AlbumManager.GetAllUid().ToList();

                if (existingTag.customInfo != null) existingTag.customInfo.music_list = uids.ToIl2Cpp();

                if (existingTag.m_MusicUids != null)
                {
                    existingTag.m_MusicUids.Clear();
                    foreach (var uid in uids) existingTag.m_MusicUids.Add(uid);
                }

                if (existingTag.m_DisplayMusicUids != null)
                    foreach (var d in existingTag.m_DisplayMusicUids)
                        if (d.musicUids != null)
                        {
                            d.musicUids.Clear();
                            foreach (var uid in uids) d.musicUids.Add(uid);
                        }

                Logger.Msg($"Tag updated ({uids.Count} albums)");
            }
        }
        catch (Exception ex)
        {
            Logger.Warning($"Tag rebuild failed: {ex.Message}");
        }
    }

    private static void RefreshUI()
    {
        try
        {
            var stage = Object.FindObjectOfType<PnlStage>();
            if (stage != null && stage.gameObject.activeInHierarchy)
            {
                stage.RefreshStageUI();
                Logger.Msg("UI refreshed");
            }
        }
        catch (Exception ex)
        {
            Logger.Warning($"Failed to refresh UI: {ex.Message}");
        }
    }

    internal static void FixedUpdate()
    {
        if (AlbumsToAdd.IsEmpty && AlbumsToDelete.IsEmpty) return;

        if (PnlStageInstance == null) return;

        if (Time.unscaledTime - _lastProcessTime < 0.02f) return;
        _lastProcessTime = Time.unscaledTime;

        var oldSelectedUid = DataHelper.selectedMusicUidFromInfoList;
        var oldSelectedAlbumName = AlbumManager.GetAlbumNameFromUid(oldSelectedUid);

        var deletedCount = ProcessDeletions();
        var addedCount = 0;
        if (deletedCount == 0) addedCount = ProcessAdditions();

        if (deletedCount > 0 || addedCount > 0)
        {
            PnlStageInstance?.m_MusicRootAnimator?.Play(PnlStageInstance.animNameAlbumIn);

            if (!string.IsNullOrEmpty(oldSelectedAlbumName) && oldSelectedUid != "0-0")
            {
                if (AlbumManager.LoadedAlbums.TryGetValue(oldSelectedAlbumName, out var newAlbum))
                {
                    var newUid = $"{AlbumManager.Uid}-{newAlbum.Index}";
                    if (newUid != oldSelectedUid)
                    {
                        DataHelper.selectedMusicUidFromInfoList = newUid;
                        Logger.Msg($"Updated selected UID from {oldSelectedUid} to {newUid}");
                    }
                }
                else
                {
                    DataHelper.selectedMusicUidFromInfoList = "0-0";
                    Logger.Msg("Selected album was deleted. Resetting selection to 0-0");
                }
            }

            RebuildCustomAlbumsTag();
            RefreshUI();
        }
    }

    internal static void OnLateInitializeMelon()
    {
        try
        {
            var watchPath = Path.GetFullPath(AlbumManager.SearchPath);
            Logger.Msg($"Watching directory: {watchPath}");

            if (!Directory.Exists(watchPath))
            {
                Logger.Warning($"Directory does not exist: {watchPath}");
                return;
            }

            AlbumManager.AlbumWatcher.Path = watchPath;
            AlbumManager.AlbumWatcher.IncludeSubdirectories = true;

            AlbumManager.AlbumWatcher.Created += (_, e) =>
            {
                var topLevelName = GetTopLevelName(e.Name);
                var fullPath = Path.Combine(watchPath, topLevelName);

                var now = DateTime.Now;
                if (LastFileEvent.TryGetValue(topLevelName, out var lastTime) &&
                    (now - lastTime).TotalMilliseconds < 500) return;
                LastFileEvent[topLevelName] = now;

                var isMdm = topLevelName.EndsWith(".mdm");
                if (!isMdm && !Directory.Exists(fullPath)) return;

                Logger.Msg($"Detected new file or folder: {topLevelName}");
                Task.Run(() =>
                {
                    var attempts = 0;
                    while (!IsFileUnlocked(fullPath) && attempts < 50)
                    {
                        Thread.Sleep(200);
                        attempts++;
                    }

                    if (attempts < 50)
                    {
                        AlbumsToAdd.Enqueue(fullPath);
                        Logger.Msg($"Queued for addition: {topLevelName}");
                    }
                    else
                    {
                        Logger.Warning($"Timed out waiting for file: {topLevelName}");
                    }
                });
            };

            AlbumManager.AlbumWatcher.Deleted += (_, e) =>
            {
                var topLevelName = GetTopLevelName(e.Name);
                var isInnerFile = e.Name != topLevelName;
                var fullPath = Path.Combine(watchPath, topLevelName);

                var now = DateTime.Now;
                if (LastFileEvent.TryGetValue(topLevelName, out var lastTime) &&
                    (now - lastTime).TotalMilliseconds < 500) return;
                LastFileEvent[topLevelName] = now;

                var isMdm = topLevelName.EndsWith(".mdm");
                var albumKey = isMdm ? $"album_{Path.GetFileNameWithoutExtension(topLevelName)}" : $"album_{topLevelName}_folder";
                if (!isMdm && !AlbumManager.LoadedAlbums.ContainsKey(albumKey)) return;

                if (isInnerFile)
                {
                    Logger.Msg($"Detected inner deletion: {e.Name}, reloading {topLevelName}");
                    Task.Run(() =>
                    {
                        var attempts = 0;
                        while (!IsFileUnlocked(fullPath) && attempts < 50)
                        {
                            Thread.Sleep(200);
                            attempts++;
                        }
                        if (attempts < 50)
                        {
                            AlbumsToDelete.Enqueue(albumKey);
                            AlbumsToAdd.Enqueue(fullPath);
                            Logger.Msg($"Queued for reload: {topLevelName}");
                        }
                    });
                }
                else
                {
                    Logger.Msg($"Detected deletion: {topLevelName}");
                    AlbumsToDelete.Enqueue(albumKey);
                }
            };

            AlbumManager.AlbumWatcher.Changed += (_, e) =>
            {
                if (e.ChangeType != WatcherChangeTypes.Changed) return;
                var topLevelName = GetTopLevelName(e.Name);
                var fullPath = Path.Combine(watchPath, topLevelName);

                var now = DateTime.Now;
                if (LastFileEvent.TryGetValue(topLevelName, out var lastTime) &&
                    (now - lastTime).TotalMilliseconds < 500) return;
                LastFileEvent[topLevelName] = now;

                var isMdm = topLevelName.EndsWith(".mdm");
                var albumKey = isMdm ? $"album_{Path.GetFileNameWithoutExtension(topLevelName)}" : $"album_{topLevelName}_folder";
                if (!isMdm && !AlbumManager.LoadedAlbums.ContainsKey(albumKey)) return;

                Logger.Msg($"Detected change: {e.Name}");
                Task.Run(() =>
                {
                    var attempts = 0;
                    while (!IsFileUnlocked(fullPath) && attempts < 50)
                    {
                        Thread.Sleep(200);
                        attempts++;
                    }

                    if (attempts < 50)
                    {
                        AlbumsToDelete.Enqueue(albumKey);
                        AlbumsToAdd.Enqueue(fullPath);
                        Logger.Msg($"Queued for reload: {topLevelName}");
                    }
                    else
                    {
                        Logger.Warning($"Timed out waiting for file: {topLevelName}");
                    }
                });
            };

            AlbumManager.AlbumWatcher.Renamed += (_, e) =>
            {
                var oldTopLevelName = GetTopLevelName(e.OldName);
                var newTopLevelName = GetTopLevelName(e.Name);
                var isInnerRename = e.OldName != oldTopLevelName || e.Name != newTopLevelName;
                var fullPath = Path.Combine(watchPath, newTopLevelName);
                
                var now = DateTime.Now;
                if (LastFileEvent.TryGetValue(newTopLevelName, out var lastTime) &&
                    (now - lastTime).TotalMilliseconds < 500) return;
                LastFileEvent[newTopLevelName] = now;

                if (isInnerRename)
                {
                    var isMdm = newTopLevelName.EndsWith(".mdm");
                    var albumKey = isMdm ? $"album_{Path.GetFileNameWithoutExtension(newTopLevelName)}" : $"album_{newTopLevelName}_folder";
                    if (!isMdm && !AlbumManager.LoadedAlbums.ContainsKey(albumKey)) return;

                    Logger.Msg($"Detected inner rename: {e.OldName} -> {e.Name}, reloading {newTopLevelName}");
                    Task.Run(() =>
                    {
                        var attempts = 0;
                        while (!IsFileUnlocked(fullPath) && attempts < 50)
                        {
                            Thread.Sleep(200);
                            attempts++;
                        }
                        if (attempts < 50)
                        {
                            AlbumsToDelete.Enqueue(albumKey);
                            AlbumsToAdd.Enqueue(fullPath);
                            Logger.Msg($"Queued for reload: {newTopLevelName}");
                        }
                    });
                    return;
                }

                var isOldMdm = oldTopLevelName.EndsWith(".mdm");
                var isNewMdm = newTopLevelName.EndsWith(".mdm");
                
                var oldKey = isOldMdm ? $"album_{Path.GetFileNameWithoutExtension(oldTopLevelName)}" : $"album_{oldTopLevelName}_folder";
                var newKey = isNewMdm ? $"album_{Path.GetFileNameWithoutExtension(newTopLevelName)}" : $"album_{newTopLevelName}_folder";

                if (!isOldMdm && !isNewMdm && !AlbumManager.LoadedAlbums.ContainsKey(oldKey) && !Directory.Exists(fullPath)) return;

                Logger.Msg($"Detected rename: {oldTopLevelName} -> {newTopLevelName}");

                if (AlbumManager.LoadedAlbums.Remove(oldKey, out var album))
                {
                    AlbumManager.LoadedAlbums.TryAdd(newKey, album);
                    AssetPatch.ModifyCacheKey($"{oldKey}_demo", $"{newKey}_demo");
                    AssetPatch.ModifyCacheKey($"{oldKey}_music", $"{newKey}_music");
                    AssetPatch.ModifyCacheKey($"{oldKey}_cover", $"{newKey}_cover");
                    Logger.Msg($"Renamed {oldKey} -> {newKey}");
                }
                else
                {
                    Logger.Msg($"Old file was not loaded, treating as new file: {newTopLevelName}");
                    Task.Run(() =>
                    {
                        var attempts = 0;
                        while (!IsFileUnlocked(fullPath) && attempts < 50)
                        {
                            Thread.Sleep(200);
                            attempts++;
                        }

                        if (attempts < 50)
                        {
                            AlbumsToAdd.Enqueue(fullPath);
                            Logger.Msg($"Queued for addition: {newTopLevelName}");
                        }
                        else
                        {
                            Logger.Warning($"Timed out waiting for file: {newTopLevelName}");
                        }
                    });
                }
            };

            AlbumManager.AlbumWatcher.EnableRaisingEvents = true;
            Logger.Msg("FileSystemWatcher initialized!");
        }
        catch (Exception ex)
        {
            Logger.Warning($"Init failed: {ex.Message}");
            Logger.Warning(ex.StackTrace);
        }
    }

    [HarmonyPatch(typeof(DBMusicTag), nameof(DBMusicTag.GetMusicInfoFromAll))]
    internal static class GetMusicInfoFromAllPatch
    {
        private static void Postfix(string musicUid, ref MusicInfo __result)
        {
            if (string.IsNullOrEmpty(musicUid)) return;

            if (HotLoadedMusicInfos.TryGetValue(musicUid, out var hotMusicInfo)) __result = hotMusicInfo;
        }
    }

    [HarmonyPatch(typeof(PnlStage), nameof(PnlStage.PreWarm))]
    internal static class StagePreWarmPatch
    {
        private static void Postfix(PnlStage __instance)
        {
            PnlStageInstance = __instance;
        }
    }
}