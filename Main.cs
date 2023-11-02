using CustomAlbums.Data;
using CustomAlbums.Managers;
using CustomAlbums.Patches;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.Nice.Interface;
using Il2CppGameCore.Scene.Controllers;
using MelonLoader;
using System.Reflection.Metadata.Ecma335;
using UnityEngine;
using static CustomAlbums.Patches.AnimatedCoverPatch;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums
{
    public class Main : MelonMod
    {
        private static readonly Logger Logger = new(nameof(Main));
        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            AssetPatch.AttachHook();
            ModSettings.Register();
            AlbumManager.LoadAlbums();
        }

        /// <summary>
        /// This override adds support for animated covers.
        /// </summary>
        public override void OnUpdate()
        {
            MusicStageCellPatch.AnimateCovers();
        }
    }
}