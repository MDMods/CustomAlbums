using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using CustomAlbums.Data;
using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.GeneralLocalization;
using Il2CppAssets.Scripts.PeroTools.Managers;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppPeroTools2.Resources;
using MelonLoader.NativeUtils;
using UnityEngine;
using AudioManager = CustomAlbums.Managers.AudioManager;
using Logger = CustomAlbums.Utilities.Logger;
using Object = UnityEngine.Object;

namespace CustomAlbums.Patches
{
    internal class AssetPatch
    {
        private const string LoadFromNameStr = "MethodInfoStoreGeneric_LoadFromName_Public_T_String_0`1";

        private static readonly NativeHook<LoadFromNameDelegate> Hook = new();
        private static readonly Dictionary<string, Object> AssetCache = new();
        private static readonly Dictionary<string, Func<string, IntPtr, string, IntPtr>> AssetHandler = new();
        private static readonly Logger Logger = new(nameof(AssetPatch));

        internal static void InitializeHandler()
        {
            AssetHandler.Add("albums", (assetName, assetPtr, _) =>
            {
                var jsonArray = Json.Deserialize<JsonArray>(new TextAsset(assetPtr).text);
                jsonArray.Add(new
                {
                    uid = AlbumManager.MusicPackage,
                    title = "Custom Albums",
                    prefabsName = $"AlbumDisco{AlbumManager.Uid}",
                    price = "¥25.00",
                    jsonName = AlbumManager.JsonName,
                    needPurchase = false,
                    free = true
                });

                var newAsset = CreateTextAsset(assetName, JsonSerializer.Serialize(jsonArray));
                if (!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(assetName))
                    Singleton<ConfigManager>.instance.Add(assetName, newAsset.text);

                AssetCache[assetName] = newAsset;
                return newAsset?.Pointer ?? assetPtr;
            });

            AssetHandler.Add(AlbumManager.JsonName, (assetName, assetPtr, _) =>
            {
                var jsonArray = new JsonArray();
                foreach (var (albumStr, albumObj) in AlbumManager.LoadedAlbums)
                {
                    var albumInfo = albumObj.Info;
                    var customChartJson = new
                    {
                        uid = $"{AlbumManager.Uid}-{albumObj.Index}",
                        name = albumInfo.Name,
                        author = albumInfo.Author,
                        bpm = albumInfo.Bpm,
                        music = $"{albumStr}_music",
                        demo = $"{albumStr}_demo",
                        cover = $"{albumStr}_cover",
                        noteJson = $"{albumStr}_map",
                        scene = albumInfo.Scene,
                        unlockLevel = "0",
                        levelDesigner = albumInfo.LevelDesigner,
                        levelDesigner1 = albumInfo.LevelDesigner1 ?? albumInfo.LevelDesigner,
                        levelDesigner2 = albumInfo.LevelDesigner2 ?? albumInfo.LevelDesigner,
                        levelDesigner3 = albumInfo.LevelDesigner3 ?? albumInfo.LevelDesigner,
                        levelDesigner4 = albumInfo.LevelDesigner4 ?? albumInfo.LevelDesigner,
                        difficulty1 = albumInfo.Difficulty1 ?? "0",
                        difficulty2 = albumInfo.Difficulty2,
                        difficulty3 = albumInfo.Difficulty3 ?? "0",
                        difficulty4 = albumInfo.Difficulty4 ?? "0"
                    };
                    jsonArray.Add(customChartJson);

                    var config = Singleton<ConfigManager>.instance.GetConfigObject<DBConfigMusicSearchTag>();
                    var searchTag = new MusicSearchTagInfo
                    {
                        uid = customChartJson.uid,
                        listIndex = config.count
                    };

                    var tags = new List<string> { "custom albums" };
                    if (albumInfo.SearchTags != null) tags.AddRange(albumInfo.SearchTags);
                    if (!string.IsNullOrEmpty(albumInfo.NameRomanized)) tags.Add(albumInfo.NameRomanized);
                    for (var i = 0; i < tags.Count; i++) tags[i] = tags[i].ToLower();

                    searchTag.tag = new Il2CppStringArray(tags.ToArray());

                    config.m_Dictionary.Add(searchTag.uid, searchTag);
                    config.list.Add(searchTag);
                }

                var newAsset = CreateTextAsset(assetName, JsonSerializer.Serialize(jsonArray));

                if (!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(assetName))
                    Singleton<ConfigManager>.instance.Add(assetName, newAsset.text);

                AssetCache[assetName] = newAsset;
                return newAsset?.Pointer ?? assetPtr;
            });

            AssetHandler.Add("albums_", (assetName, assetPtr, language) =>
            {
                var jsonArray = Json.Deserialize<JsonArray>(new TextAsset(assetPtr).text);
                jsonArray.Add(new
                {
                    title = AlbumManager.Languages[language],
                });

                var newAsset = CreateTextAsset(assetName, JsonSerializer.Serialize(jsonArray));

                if (!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(assetName))
                    Singleton<ConfigManager>.instance.Add(assetName, newAsset.text);

                AssetCache[assetName] = newAsset;
                return newAsset?.Pointer ?? assetPtr;
            });

            AssetHandler.Add($"{AlbumManager.JsonName}_", (assetName, assetPtr, _) =>
            {
                var jsonArray = new JsonArray();

                foreach (var (_, value) in AlbumManager.LoadedAlbums)
                    jsonArray.Add(new
                    {
                        name = value.Info.Name,
                        author = value.Info.Author
                    });

                var newAsset = CreateTextAsset(assetName, JsonSerializer.Serialize(jsonArray));
                if (!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(assetName))
                    Singleton<ConfigManager>.instance.Add(assetName, newAsset.text);

                AssetCache[assetName] = newAsset;
                return newAsset?.Pointer ?? assetPtr;
            });

            AssetHandler.Add("album_", (assetName, assetPtr, _) =>
            {
                var cache = true;
                Object newAsset = null;
                var suffix = AssetIdentifiers.AssetSuffixes.FirstOrDefault(assetName.EndsWith);
                if (!string.IsNullOrEmpty(suffix))
                {
                    var albumKey = assetName.Remove(assetName.Length - suffix.Length);
                    AlbumManager.LoadedAlbums.TryGetValue(albumKey, out var album);
                    if (suffix.StartsWith("_map"))
                    {
                        newAsset = album?.Sheets[int.Parse(suffix[^1].ToString())].StageInfo;
                        cache = false;
                    }
                    else
                    {
                        switch (suffix)
                        {
                            case "_demo":
                                newAsset = album?.Demo;
                                break;
                            case "_music":
                                newAsset = album?.Music;
                                break;
                            case "_cover":
                                newAsset = album?.Cover;
                                break;
                            default:
                                Logger.Error($"Unknown suffix: {suffix}");
                                break;
                        }
                    }
                } 

                if (cache) AssetCache[assetName] = newAsset;
                return newAsset?.Pointer ?? assetPtr;
            });
        }

        internal static unsafe void AttachHook()
        {
            var type = typeof(ResourcesManager)
                .GetNestedType(LoadFromNameStr, BindingFlags.NonPublic)?
                .MakeGenericType(typeof(TextAsset));

            if (type is null)
            {
                Logger.Error("FATAL ERROR: AssetPatch failed.");
                return;
            }

            InitializeHandler();

            var originalLfn = *(IntPtr*)(IntPtr)type
                .GetField("Pointer", BindingFlags.NonPublic | BindingFlags.Static)
                ?.GetValue(type)!;

            // create a pointer for our new method to be called instead
            // this is Cdecl because this is going to be called in an unmanaged context
            delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr> detourPtr = &LoadFromNamePatch;

            // set the hook so that LoadFromNamePatch runs instead of the original LoadFromName
            Hook.Detour = (IntPtr)detourPtr;
            Hook.Target = originalLfn;
            Hook.Attach();
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static IntPtr LoadFromNamePatch(IntPtr instance, IntPtr assetNamePtr, IntPtr nativeMethodInfo)
        {
            var assetPtr = Hook.Trampoline(instance, assetNamePtr, nativeMethodInfo);
            var assetName = IL2CPP.Il2CppStringToManaged(assetNamePtr) ?? "";
            Logger.Msg($"Loading {assetName}!");

            if (AssetCache.TryGetValue(assetName, out var cachedAsset))
            {
                if (cachedAsset != null)
                {
                    Logger.Msg($"Returning {assetName} from cache");
                    AudioManager.SwitchLoad(assetName);
                    return cachedAsset.Pointer;
                }
                Logger.Msg("Removing null asset from cache");
                AssetCache.Remove(assetName);
            }

            if (assetName == "LocalizationSettings") return assetPtr;

            var language = SingletonScriptableObject<LocalizationSettings>.instance.GetActiveOption("Language");

            var handledAssetName = assetName.StartsWith("albums_") ? "albums_" : assetName;
            handledAssetName = handledAssetName.StartsWith($"{AlbumManager.JsonName}_")
                ? $"{AlbumManager.JsonName}_"
                : handledAssetName;
            handledAssetName = handledAssetName.StartsWith("album_") ? "album_" : handledAssetName;

            return AssetHandler.TryGetValue(handledAssetName, out var value)
                ? value(assetName, assetPtr, language)
                : assetPtr;
        }

        private static TextAsset CreateTextAsset(string name, string text)
        {
            var asset = new TextAsset(text)
            {
                name = name
            };
            return asset;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr LoadFromNameDelegate(IntPtr instance, IntPtr assetNamePtr, IntPtr nativeMethodInfo);
    }
}