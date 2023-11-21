using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using CustomAlbums.Data;
using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
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

        /// <summary>
        /// Adds methods to the <c>AssetHandler</c>.
        /// The <c>AssetHandler</c> modifies certain assets based on their name.
        /// </summary>
        internal static void InitializeHandler()
        {
            AssetHandler.Add("albums", (assetName, assetPtr, _) =>
            {
                var jsonArray = Json.Deserialize<JsonArray>(new TextAsset(assetPtr).text);

                // adds the CustomAlbums "dlc" to the game
                jsonArray.Add(new
                {
                    uid = AlbumManager.MusicPackage,
                    title = "Custom Albums",
                    prefabsName = $"AlbumDisco{AlbumManager.UID}",
                    price = "¥25.00",
                    jsonName = AlbumManager.JsonName,
                    needPurchase = false,
                    free = true
                });

                // create and add the new asset with the CustomAlbums "dlc"
                var newAsset = CreateTextAsset(assetName, JsonSerializer.Serialize(jsonArray));
                if (!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(assetName))
                    Singleton<ConfigManager>.instance.Add(assetName, newAsset.text);

                // set cache and return newAsset's pointer if it non-null
                AssetCache[assetName] = newAsset;
                return newAsset?.Pointer ?? assetPtr;
            });

            AssetHandler.Add(AlbumManager.JsonName, (assetName, assetPtr, _) =>
            {
                var jsonArray = new JsonArray();

                // add each custom charts' data to the album
                foreach (var (albumStr, albumObj) in AlbumManager.LoadedAlbums)
                {
                    var albumInfo = albumObj.Info;
                    var customChartJson = new
                    {
                        uid = $"{AlbumManager.UID}-{albumObj.Index}",
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

                    // configure the searchtaginfo
                    var config = Singleton<ConfigManager>.instance.GetConfigObject<DBConfigMusicSearchTag>();
                    var searchTag = new MusicSearchTagInfo
                    {
                        uid = customChartJson.uid,
                        listIndex = config.count
                    };
                     
                    // create the search tag itself
                    var tags = new List<string> { "custom albums" };
                    if (albumInfo.SearchTags != null) tags.AddRange(albumInfo.SearchTags);
                    if (!string.IsNullOrEmpty(albumInfo.NameRomanized)) tags.Add(albumInfo.NameRomanized);
                    for (var i = 0; i < tags.Count; i++) tags[i] = tags[i].ToLower();

                    searchTag.tag = new Il2CppStringArray(tags.ToArray());

                    // add it to the game
                    config.m_Dictionary.Add(searchTag.uid, searchTag);
                    config.list.Add(searchTag);
                }

                // create and add the new asset with the custom charts' data 
                var newAsset = CreateTextAsset(assetName, JsonSerializer.Serialize(jsonArray));
                if (!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(assetName))
                    Singleton<ConfigManager>.instance.Add(assetName, newAsset.text);

                // set cache and return newAsset's pointer if it non-null
                AssetCache[assetName] = newAsset;
                return newAsset?.Pointer ?? assetPtr;
            });

            AssetHandler.Add("albums_", (assetName, assetPtr, language) =>
            {
                var jsonArray = Json.Deserialize<JsonArray>(new TextAsset(assetPtr).text);

                // adds the correct language for the "Custom Albums" album
                jsonArray.Add(new
                {
                    title = AlbumManager.Languages[language],
                });

                // create and add the new asset with the correct lingual name of "Custom Albums"
                var newAsset = CreateTextAsset(assetName, JsonSerializer.Serialize(jsonArray));
                if (!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(assetName))
                    Singleton<ConfigManager>.instance.Add(assetName, newAsset.text);

                // set cache and return newAsset's pointer if it non-null
                AssetCache[assetName] = newAsset;
                return newAsset?.Pointer ?? assetPtr;
            });

            AssetHandler.Add($"{AlbumManager.JsonName}_", (assetName, assetPtr, _) =>
            {
                var jsonArray = new JsonArray();

                // this should technically be written to retreive and add the name of the chart based on your selected language
                // however this only grabs whatever name is given in the info.json
                // very likely not a big deal
                foreach (var (_, value) in AlbumManager.LoadedAlbums)
                    jsonArray.Add(new
                    {
                        name = value.Info.Name,
                        author = value.Info.Author
                    });

                // create and add the new asset with the names of each custom chart
                var newAsset = CreateTextAsset(assetName, JsonSerializer.Serialize(jsonArray));
                if (!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(assetName))
                    Singleton<ConfigManager>.instance.Add(assetName, newAsset.text);

                // set cache and return newAsset's pointer if it non-null
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
                        newAsset = album?.Sheets[int.Parse(suffix[^1].ToString())].GetStage();
                        // do not cache the StageInfos, this should be loaded into memory only when we need it
                        cache = false;
                    }
                    else
                    {
                        // set the newAsset to the objects held in the album
                        switch (suffix)
                        {
                            case "_demo":
                                newAsset = album?.Demo;
                                break;
                            case "_music":
                                newAsset = album?.Music;
                                break;
                            case "_cover":
                                newAsset = album?.AnimatedCover?.Frames[0] ?? album?.Cover;
                                break;
                            default:
                                Logger.Error($"Unknown suffix: {suffix}");
                                break;
                        }
                    }
                }

                // set cache if we should and return newAsset's pointer if it non-null
                if (cache) AssetCache[assetName] = newAsset;
                return newAsset?.Pointer ?? assetPtr;
            });
        }

        /// <summary>
        /// Gets <c>LoadFromName&lt;TextAsset&gt;</c> and detours it using a 
        /// <c>NativeHook&lt;LoadFromNameDelegate&gt;</c> to <c>LoadFromNamePatch</c>.
        /// </summary>
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

            // AttachHook should only be ran once; create the handler
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


        /// <summary>
        /// Modifies certain game data as it get loaded.
        /// The game data that is modified directly adds support for custom albums.
        /// </summary>
        /// <param name="instance">The instance of ResourceManager calling LoadFromName.</param>
        /// <param name="assetNamePtr">The pointer to the string assetName.</param>
        /// <param name="nativeMethodInfo">Method info used by the orignal method.</param>
        /// <returns>A pointer of either a newly created asset if it exists or the original asset pointer if a new one was not created.</returns>
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static IntPtr LoadFromNamePatch(IntPtr instance, IntPtr assetNamePtr, IntPtr nativeMethodInfo)
        {
            // retrieve the pointer of the asset and the name of the asset
            var assetPtr = Hook.Trampoline(instance, assetNamePtr, nativeMethodInfo);
            var assetName = IL2CPP.Il2CppStringToManaged(assetNamePtr) ?? string.Empty;
            
            Logger.Msg($"Loading {assetName}!");

            // if the asset exists in the cache then retrieve it
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

            // allow original LoadFromName to run with LocalizationSettings
            if (assetName == "LocalizationSettings") return assetPtr;

            var language = SingletonScriptableObject<LocalizationSettings>.instance.GetActiveOption("Language");

            // programmatically alters the asset name to remove the language if the language exists 
            var handledAssetName = assetName.StartsWith("albums_") ? "albums_" : assetName;
            handledAssetName = handledAssetName.StartsWith($"{AlbumManager.JsonName}_")
                ? $"{AlbumManager.JsonName}_"
                : handledAssetName;
            handledAssetName = handledAssetName.StartsWith("album_") ? "album_" : handledAssetName;

            // get the method from the AssetHandler if it exists and run it, otherwise return assetPtr
            return AssetHandler.TryGetValue(handledAssetName, out var value)
                ? value(assetName, assetPtr, language)
                : assetPtr;
        }

        /// <summary>
        /// Simple helper method to create a TextAsset.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="text"></param>
        /// <returns>A new TextAsset initialized with the parameters.</returns>
        private static TextAsset CreateTextAsset(string name, string text)
        {
            return new TextAsset(text)
            {
                name = name
            };
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr LoadFromNameDelegate(IntPtr instance, IntPtr assetNamePtr, IntPtr nativeMethodInfo);
    }
}