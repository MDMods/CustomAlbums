using CustomAlbums.Patches;
using CustomAlbums.Utilities;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.UI.Panels;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2Cpp;
using Il2CppNewtonsoft.Json.Serialization;
using UnityEngine;
using Logger = CustomAlbums.Utilities.Logger;
using System.Text.Json.Nodes;
using Il2CppAssets.Scripts.Database;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.IO;
using Il2CppAssets.Scripts.PeroTools.Managers;
using Il2CppSystem.Resources;
using Il2CppPeroTools2.Resources;

namespace CustomAlbums.Managers
{
    internal static class HotReloadManager
    {
        private static readonly Logger Logger = new(nameof(HotReloadManager));
        private static bool Update { get; set; }
        private static Queue<string> AlbumsAdded { get; } = new();
        private static Queue<string> AlbumsDeleted { get; } = new();
        private static PnlStage PnlStageInstance { get; set; }
        private static bool IsFileUnlocked(string path)
        {
            try
            {
                using var fileStream = File.Open(path, FileMode.Open);
                if (fileStream.Length > 0)
                {
                    Logger.Msg("The added album is ready to be read!");
                    return true;
                }
                return false;
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is IOException)
            {
                return false;
            }
        }

        private static void RemoveAllCachedAssets(string albumName)
        {
            Logger.Msg("Removing " + albumName + "!");
            AlbumManager.LoadedAlbums.Remove($"album_{albumName}");
            AssetPatch.RemoveFromCache($"{albumName}_demo");
            AssetPatch.RemoveFromCache($"{albumName}_music");
            AssetPatch.RemoveFromCache($"{albumName}_cover");
            Logger.Msg("Sucessfully removed from cache!");
        }

        private static void RenameAllCachedAssets(string oldAlbumName, string newAlbumName)
        {
            Logger.Msg($"Renaming {oldAlbumName} to {newAlbumName}!");
            var success = AlbumManager.LoadedAlbums.Remove(oldAlbumName, out var album);
            if (!success) return;
            AlbumManager.LoadedAlbums.TryAdd(newAlbumName, album); 
            AssetPatch.ModifyCacheKey($"{oldAlbumName}_demo", $"{newAlbumName}_demo");
            AssetPatch.ModifyCacheKey($"{oldAlbumName}_music", $"{newAlbumName}_music");
            AssetPatch.ModifyCacheKey($"{oldAlbumName}_cover", $"{newAlbumName}_cover");
            Logger.Msg("Sucessfully modified cache!");
        }

        /// <summary>
        /// Runs the logic for hot reloading on Unity's FixedUpdate
        /// </summary>
        internal static void FixedUpdate() 
        {
            while (AlbumsAdded.Count > 0)
            {
                AlbumManager.LoadOne(AlbumsAdded.Dequeue());
            }
            while (AlbumsDeleted.Count > 0)
            {
                RemoveAllCachedAssets(AlbumsDeleted.Dequeue());
            }
            if (Update)
            {
                // TODO: implement update logic   
            }

        }

        /// <summary>
        /// Initalizes the AlbumWatcher for adding/deleting/renaming new custom charts.
        /// </summary>
        internal static void OnLateInitializeMelon()
        {
            AlbumManager.AlbumWatcher.Path = AlbumManager.SEARCH_PATH;
            AlbumManager.AlbumWatcher.Filter = AlbumManager.SEARCH_PATTERN;

            AlbumManager.AlbumWatcher.Created += (s, e) =>
            {
                Logger.Msg("Added file " + e.Name);
                while (!IsFileUnlocked(e.FullPath)) { }
                AlbumsAdded.Enqueue(e.FullPath);
                Update = true;
            };
            AlbumManager.AlbumWatcher.Deleted += (s, e) =>
            {
                Logger.Msg("Deleted file " + e.Name);
                AlbumsDeleted.Enqueue(Path.GetFileNameWithoutExtension(e.Name));
                Update = true;
            };
            AlbumManager.AlbumWatcher.Renamed += (s, e) =>
            {
                Logger.Msg("Renamed file " + e.OldName + " -> " + e.Name);
                RenameAllCachedAssets($"album_{Path.GetFileNameWithoutExtension(e.OldName)}", $"album_{Path.GetFileNameWithoutExtension(e.Name)}");
                Update = true;
            };

            AlbumManager.AlbumWatcher.EnableRaisingEvents = true;
        }

        [HarmonyPatch(typeof(PnlStage), nameof(PnlStage.PreWarm))]
        internal static class StagePreWarmPatch
        {
            private static void Postfix(PnlStage __instance)
            {
                Logger.Msg($"PnlStage instance retrieved. Null = {__instance == null}");
                PnlStageInstance = __instance;
            }
        }
    }

}
