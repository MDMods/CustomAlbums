﻿using CustomAlbums.Utilities;
using HarmonyLib;
using Il2CppGameLogic;

namespace CustomAlbums.Patches
{
    internal class MikuSceneSupportPatch
    {

        /// <summary>
        ///     Fixes the purple background issue when using the BMS ID "1X" to switch to the Miku scene.
        /// </summary>
        [HarmonyPatch(typeof(GameMusicScene), nameof(GameMusicScene.SceneFestival))]
        internal class MikuFixPatch
        {
            private static void Finalizer(string sceneFestivalName, ref string __result)
            {
                var subStr = sceneFestivalName[6..];
                
                if (subStr.Length <= 2 || !subStr.TryParseAsInt(out var sceneIndex)) return;
                
                // For some reason, scene change 1X sets scene to scene_010, which is invalid
                // Just trim off "scene_" and parse the numbers after the underscore to int again to get 10 instead of 010
                __result = $"scene_{sceneIndex}";
            }
        }
    }
}
