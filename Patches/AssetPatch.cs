using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using BetterNativeHook;
using CustomAlbums.Data;
using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.GeneralLocalization;
using Il2CppAssets.Scripts.PeroTools.Managers;
using Il2CppInterop.Common;
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
        private static readonly Dictionary<string, Object> AssetCache = new();
        /// <summary>
        ///     Modifies certain assets based on their name.
        /// </summary>
        private static readonly Dictionary<string, Func<string, IntPtr, string, IntPtr>> AssetHandler = new();
        private static readonly Logger Logger = new(nameof(AssetPatch));

        private static int _latestAlbumNum;
        private static bool _doneLoadingAlbumsFlag;

        /// <summary>
        ///     Binds the asset to a new key, while removing the old asset.
        /// </summary>
        /// <param name="oldAssetName">The old assetName.</param>
        /// <param name="newAssetName">The new assetName where the value should be bound.</param>
        /// <returns>true if the key was successfully bound; otherwise, false.</returns>
        internal static bool ModifyCacheKey(string oldAssetName, string newAssetName)
        {
            var success = AssetCache.Remove(oldAssetName, out var asset);
            return success && AssetCache.TryAdd(newAssetName, asset);
        }

        /// <summary>
        ///     Removes the specified key from the asset cache.
        /// </summary>
        /// <param name="key">The key corresponding to the value to be removed.</param>
        /// <returns>true if the entry was successfully removed; otherwise, false.</returns>
        internal static bool RemoveFromCache(string key)
        {
            return AssetCache.Remove(key);
        }

        /// <summary>
        ///     Adds methods to the <see cref="AssetHandler"/>.
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
                    prefabsName = $"AlbumDisco{AlbumManager.Uid}",
                    price = "¥25.00",
                    jsonName = AlbumManager.JsonName,
                    needPurchase = false,
                    free = true
                });

                // Create and add the new asset with the CustomAlbums "dlc"
                var newAsset = CreateTextAsset(assetName, JsonSerializer.Serialize(jsonArray));
                if (!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(assetName))
                    Singleton<ConfigManager>.instance.Add(assetName, newAsset.text);

                // Set cache and return newAsset's pointer if it non-null
                AssetCache[assetName] = newAsset;
                return newAsset?.Pointer ?? assetPtr;
            });

            AssetHandler.Add(AlbumManager.JsonName, (assetName, assetPtr, _) =>
            {
                var jsonArray = new JsonArray();

                // Add each custom charts' data to the album
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

                    // Configure the searchtaginfo
                    var config = Singleton<ConfigManager>.instance.GetConfigObject<DBConfigMusicSearchTag>();
                    var searchTag = new MusicSearchTagInfo
                    {
                        uid = customChartJson.uid,
                        listIndex = config.count
                    };

                    // Create the search tag itself
                    var tags = new List<string> { "custom albums" };
                    if (albumInfo.SearchTags != null) tags.AddRange(albumInfo.SearchTags);
                    if (!string.IsNullOrEmpty(albumInfo.NameRomanized)) tags.Add(albumInfo.NameRomanized);
                    for (var i = 0; i < tags.Count; i++) tags[i] = tags[i].ToLower();

                    searchTag.tag = new Il2CppStringArray(tags.ToArray());

                    // Add it to the game
                    config.m_Dictionary.Add(searchTag.uid, searchTag);
                    config.list.Add(searchTag);
                }

                // Create and add the new asset with the custom charts' data 
                var newAsset = CreateTextAsset(assetName, JsonSerializer.Serialize(jsonArray));
                if (!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(assetName))
                    Singleton<ConfigManager>.instance.Add(assetName, newAsset.text);

                // Set cache and return newAsset's pointer if it non-null
                AssetCache[assetName] = newAsset;
                return newAsset?.Pointer ?? assetPtr;
            });

            AssetHandler.Add("albums_", (assetName, assetPtr, language) =>
            {
                var jsonArray = Json.Deserialize<JsonArray>(new TextAsset(assetPtr).text);

                // Adds the correct language for the "Custom Albums" album
                jsonArray.Add(new
                {
                    title = AlbumManager.Languages[language]
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

                // This should technically be written to retrieve and add the name of the chart based on your selected language
                // However this only grabs whatever name is given in the info.json
                // Very likely not a big deal
                foreach (var (_, value) in AlbumManager.LoadedAlbums)
                    jsonArray.Add(new
                    {
                        name = value.Info.Name,
                        author = value.Info.Author
                    });

                // Create and add the new asset with the names of each custom chart
                var newAsset = CreateTextAsset(assetName, JsonSerializer.Serialize(jsonArray));
                if (!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(assetName))
                    Singleton<ConfigManager>.instance.Add(assetName, newAsset.text);

                // Set cache and return newAsset's pointer if it non-null
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
                        newAsset = album?.Sheets[int.Parse(suffix[^1].ToString(), CultureInfo.InvariantCulture)].GetStage();
                        // Do not cache the StageInfos, this should be loaded into memory only when we need it
                        cache = false;
                    }
                    else
                    {
                        // Set the newAsset to the objects held in the album
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

                // Set cache if we should and return newAsset's pointer if it non-null
                if (cache) AssetCache[assetName] = newAsset;
                return newAsset?.Pointer ?? assetPtr;
            });
        }

        static MelonHookInfo MelonHookInfo;
        static GenericNativeHook Hook;

        /// <summary>
        ///     Gets <see cref="ResourcesManager.LoadFromName{T}(string)"/> (where <typeparamref name="T"/> is <see cref="TextAsset"/>) and detours it using a <see cref="GenericNativeHook"/>
        ///     <para/>
        ///     to <see cref="HookCallback(ReturnValueReference, ReadOnlyCollection{ParameterReference})"/>.
        /// </summary>
        internal static unsafe void AttachHook()
        {
            var loadFromNameMethod = AccessTools.Method(typeof(ResourcesManager),
                nameof(ResourcesManager.LoadFromName), new[] { typeof(string) },
                new[] { typeof(TextAsset) });

            if (loadFromNameMethod is null)
            {
                Logger.Error("FATAL ERROR: AssetPatch failed.");
                Thread.Sleep(1000);
                Environment.Exit(1);
            }

            // AttachHook should only be run once; create the handler
            InitializeHandler();

            Hook = GenericNativeHook.CreateInstance(out MelonHookInfo, loadFromNameMethod);
            MelonHookInfo.HookCallbackEvent += LoadFromNamePatch;
            Hook.AttachHook();
        }



        /// <summary>
        ///     Modifies certain game data as it get loaded.
        ///     The game data that is modified directly adds support for custom albums.
        /// </summary>

        /// <param name="returnValue">An instance that represents the trampoline's return value, and changes to it.</param>
        /// <param name="parameters">A reference to the parameters of the trampoline.</param>
        /// <returns>
        ///     A pointer of either a newly created asset if it exists or the original asset pointer if a new one was not
        ///     created.
        /// </returns>
        private static void LoadFromNamePatch(ReturnValueReference returnValue, ReadOnlyCollection<ParameterReference> parameters)
        {
            /// The original pointer to the string assetName.
            IntPtr assetNamePtr = parameters[1].OriginalValue;

            // Retrieve the name of the asset
            var assetName = IL2CPP.Il2CppStringToManaged(assetNamePtr) ?? string.Empty;

            Logger.Msg($"Loading {assetName}!");

            if (assetName.StartsWith("ALBUM") && int.TryParse(assetName.AsSpan(5), NumberStyles.Number, CultureInfo.InvariantCulture, out var albumNum) &&
                albumNum != AlbumManager.Uid + 1)
            {
                // If done loading albums, we've found the maximum actual album
                // If there's an attempt to load other albums (there will be if you open the tag menu), early return zero pointer
                // This provides a noticeable speedup when opening tags for the first time
                if (_doneLoadingAlbumsFlag && albumNum > _latestAlbumNum)
                {
                    returnValue.Override = IntPtr.Zero;
                    return;
                }

                // Otherwise get the maximum number X of ALBUMX
                _latestAlbumNum = Math.Max(_latestAlbumNum, albumNum);
            }

            // If we're loading music_search_tag we're done loading albums
            if (assetName == "music_search_tag") _doneLoadingAlbumsFlag = true;

            // If the asset exists in the cache then retrieve it
            if (AssetCache.TryGetValue(assetName, out var cachedAsset))
            {
                if (cachedAsset != null)
                {
                    Logger.Msg($"Returning {assetName} from cache");
                    AudioManager.SwitchLoad(assetName);
                    returnValue.Override = cachedAsset.Pointer;
                    return;
                }

                Logger.Msg("Removing null asset from cache");
                AssetCache.Remove(assetName);
            }

            // Allow original LoadFromName to run with LocalizationSettings
            if (assetName is "LocalizationSettings") return;

            var language = SingletonScriptableObject<LocalizationSettings>.instance.GetActiveOption("Language");

            // Programmatically alters the asset name to remove the language if the language exists 
            var handledAssetName = assetName.StartsWith("albums_") ? "albums_" : assetName;
            handledAssetName = handledAssetName.StartsWith($"{AlbumManager.JsonName}_")
                ? $"{AlbumManager.JsonName}_"
                : handledAssetName;
            handledAssetName = handledAssetName.StartsWith("album_") ? "album_" : handledAssetName;

            // Get the method from the AssetHandler if it exists and run it, otherwise return false
            if (AssetHandler.TryGetValue(handledAssetName, out var value))
            {
                var trampolinePointer = returnValue.GetValueOrInvokeTrampoline();
                var newPointer = value(assetName, trampolinePointer, language);
                if (newPointer != trampolinePointer)
                {
                    returnValue.Override = newPointer;
                }
            }
        }

        /// <summary>
        ///     Simple helper method to create a TextAsset.
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
    }
}