using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CustomAlbums.Data
{
    internal class DummyMusicInfo
    {
        public DummyMusicInfo(Album albumObj, string albumStr)
        {
            var albumInfo = albumObj.Info;
            uid = albumObj.Uid;
            name = albumInfo.Name;
            author = albumInfo.Author;
            bpm = albumInfo.Bpm;
            music = $"{albumStr}_music";
            demo = $"{albumStr}_demo";
            cover = $"{albumStr}_cover";
            noteJson = $"{albumStr}_map";
            scene = albumInfo.Scene;
            unlockLevel = "0";
            levelDesigner = albumInfo.LevelDesigner;
            levelDesigner1 = albumInfo.LevelDesigner1 ?? albumInfo.LevelDesigner;
            levelDesigner2 = albumInfo.LevelDesigner2 ?? albumInfo.LevelDesigner;
            levelDesigner3 = albumInfo.LevelDesigner3 ?? albumInfo.LevelDesigner;
            levelDesigner4 = albumInfo.LevelDesigner4 ?? albumInfo.LevelDesigner;
            levelDesigner5 = albumInfo.LevelDesigner5 ?? albumInfo.LevelDesigner;
            difficulty1 = albumInfo.Difficulty1 ?? "0";
            difficulty2 = albumInfo.Difficulty2 ?? "?";
            difficulty3 = albumInfo.Difficulty3 ?? "0";
            difficulty4 = albumInfo.Difficulty4 ?? "0";
            difficulty5 = albumInfo.Difficulty5 ?? "0";
        }
        public string uid;
        public string name;
        public string author;
        public string bpm;
        public string music;
        public string demo;
        public string cover;
        public string noteJson;
        public string scene;
        public string unlockLevel;
        public string levelDesigner;
        public string levelDesigner1;
        public string levelDesigner2;
        public string levelDesigner3;
        public string levelDesigner4;
        public string levelDesigner5;
        public string difficulty1;
        public string difficulty2;
        public string difficulty3;
        public string difficulty4;
        public string difficulty5;
    }
}
