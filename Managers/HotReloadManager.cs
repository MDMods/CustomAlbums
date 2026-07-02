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
            if (!File.Exists(path)) return false;
            using var fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
            return fileStream.Length > 0;
        }
        catch (Exception ex) when (ex is FileNotFoundException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }


    private static string GetTopLevelName(string relativePath)
    {
        var separatorIndex =
            relativePath.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
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
        if (!AlbumsToAdd.TryDequeue(out var path)) return addedCount;
        
        try
        {
            var newAlbums = new List<Album>();
            if (path.EndsWith(".mdp", StringComparison.OrdinalIgnoreCase))
            {
                var pack = AlbumManager.LoadPack(path);
                if (pack != null) newAlbums.AddRange(pack.Albums);
            }
            else
            {
                var album = AlbumManager.LoadOne(path);
                if (album != null) newAlbums.Add(album);
            }

            if (newAlbums.Count == 0)
            {
                Logger.Warning($"Failed to load album(s) from {path}");
                return 0; // Abort this addition
            }

            foreach (var album in newAlbums)
            {
                var uid = $"{AlbumManager.Uid}-{album.Index}";
                Logger.Msg($"Adding {album.AlbumName} (UID: {uid})");
            }

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
                        difficulty1 = albumObj.HasDifficulty(1) ? aInfo.Difficulty1 ?? "0" : "0",
                        difficulty2 = albumObj.HasDifficulty(2) ? aInfo.Difficulty2 ?? "0" : "0",
                        difficulty3 = albumObj.HasDifficulty(3) ? aInfo.Difficulty3 ?? "0" : "0",
                        difficulty4 = albumObj.HasDifficulty(4) ? aInfo.Difficulty4 ?? "0" : "0",
                        difficulty5 = albumObj.HasDifficulty(5) ? aInfo.Difficulty5 ?? "0" : "0"
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
                    if (localDicProp.GetValue(globalAlbumConfig) is
                        Il2CppSystem.Collections.Generic.Dictionary<int, DBConfigLocalALBUM> globalLocalDic)
                    {
                        globalLocalDic.Clear();
                        for (var i = 0; i <= 15; i++)
                        {
                            var newLocalAlbum = new DBConfigLocalALBUM();
                            newLocalAlbum.Deserialize(fullLocalJsonStr);
                            globalLocalDic.Add(i, newLocalAlbum);
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

                Logger.Msg("Transmuted MusicInfo for new albums");
            }
            else
            {
                Logger.Error("globalAlbumConfig is null! Cannot inject metadata.");
            }

            foreach (var album in newAlbums)
            {
                var uid = album.Uid;
                var albumName = album.AlbumName;
                var albumInfo = album.Info;

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
            }

            addedCount += newAlbums.Count;
            Logger.Msg($"Successfully added {newAlbums.Count} albums");
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

        if (AlbumsToDelete.TryDequeue(out var path))
            try
            {
                Logger.Msg($"Processing deletion for {path}");

                var fullPath = Path.GetFullPath(path);
                var albumsToRemove = AlbumManager.LoadedAlbums.Values
                    .Where(a => Path.GetFullPath(a.Path).Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (path.EndsWith(".mdp", StringComparison.OrdinalIgnoreCase))
                {
                    PackManager.RemovePacksByPath(fullPath);
                    Logger.Msg($"Removed pack at {fullPath}");
                }

                if (albumsToRemove.Count == 0)
                {
                    Logger.Warning($"No loaded albums found for path {path}");
                    return 0;
                }

                foreach (var album in albumsToRemove)
                {
                    var albumKey = album.AlbumName;
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
                Logger.Msg("UI refreshed (without InitSpecialsMusic)");
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
        var addedCount = ProcessAdditions();

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
            // No longer tracking subdirectories, only single mdm/mdp files in Custom_Albums
            AlbumManager.AlbumWatcher.IncludeSubdirectories = false;

            AlbumManager.AlbumWatcher.Created += (_, e) =>
            {
                if (!e.Name.EndsWith(".mdm", StringComparison.OrdinalIgnoreCase) &&
                    !e.Name.EndsWith(".mdp", StringComparison.OrdinalIgnoreCase)) return;

                var now = DateTime.Now;
                if (LastFileEvent.TryGetValue(e.Name, out var lastTime) &&
                    (now - lastTime).TotalMilliseconds < 500) return;
                LastFileEvent[e.Name] = now;

                var fullPath = e.FullPath;
                Logger.Msg($"Detected new file: {e.Name}");
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
                        Logger.Msg($"Queued for addition: {e.Name}");
                    }
                    else
                    {
                        Logger.Warning($"Timed out waiting for file: {e.Name}");
                    }
                });
            };

            AlbumManager.AlbumWatcher.Deleted += (_, e) =>
            {
                if (!e.Name.EndsWith(".mdm", StringComparison.OrdinalIgnoreCase) &&
                    !e.Name.EndsWith(".mdp", StringComparison.OrdinalIgnoreCase)) return;

                var now = DateTime.Now;
                if (LastFileEvent.TryGetValue(e.Name, out var lastTime) &&
                    (now - lastTime).TotalMilliseconds < 500) return;
                LastFileEvent[e.Name] = now;

                Logger.Msg($"Detected deletion: {e.Name}");
                AlbumsToDelete.Enqueue(e.FullPath);
            };

            AlbumManager.AlbumWatcher.Changed += (_, e) =>
            {
                if (e.ChangeType != WatcherChangeTypes.Changed) return;

                if (!e.Name.EndsWith(".mdm", StringComparison.OrdinalIgnoreCase) &&
                    !e.Name.EndsWith(".mdp", StringComparison.OrdinalIgnoreCase)) return;

                var now = DateTime.Now;
                if (LastFileEvent.TryGetValue(e.Name, out var lastTime) &&
                    (now - lastTime).TotalMilliseconds < 500) return;
                LastFileEvent[e.Name] = now;

                var fullPath = e.FullPath;
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
                        AlbumsToDelete.Enqueue(fullPath);
                        AlbumsToAdd.Enqueue(fullPath);
                        Logger.Msg($"Queued for reload: {e.Name}");
                    }
                    else
                    {
                        Logger.Warning($"Timed out waiting for file: {e.Name}");
                    }
                });
            };

            AlbumManager.AlbumWatcher.Renamed += (_, e) =>
            {
                var isOldMdm = e.OldName.EndsWith(".mdm", StringComparison.OrdinalIgnoreCase);
                var isOldMdp = e.OldName.EndsWith(".mdp", StringComparison.OrdinalIgnoreCase);
                var isNewMdm = e.Name.EndsWith(".mdm", StringComparison.OrdinalIgnoreCase);
                var isNewMdp = e.Name.EndsWith(".mdp", StringComparison.OrdinalIgnoreCase);

                if (!isOldMdm && !isOldMdp && !isNewMdm && !isNewMdp) return;

                var now = DateTime.Now;
                if (LastFileEvent.TryGetValue(e.Name, out var lastTime) &&
                    (now - lastTime).TotalMilliseconds < 500) return;
                LastFileEvent[e.Name] = now;

                Logger.Msg($"Detected rename: {e.OldName} -> {e.Name}");

                // Enqueue deletion for the old path (if it was an mdm/mdp)
                if (isOldMdm || isOldMdp) AlbumsToDelete.Enqueue(e.OldFullPath);

                // Enqueue addition for the new path (if it is an mdm/mdp)
                if (isNewMdm || isNewMdp)
                {
                    var fullPath = e.FullPath;
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
                            Logger.Msg($"Queued for addition: {e.Name}");
                        }
                        else
                        {
                            Logger.Warning($"Timed out waiting for file: {e.Name}");
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