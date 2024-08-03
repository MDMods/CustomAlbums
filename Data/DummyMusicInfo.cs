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
        public string uid { get; set; }
        public string name { get; set; }
        public string author { get; set; }
        public string bpm { get; set; }
        public string music { get; set; }
        public string demo { get; set; }
        public string cover { get; set; }
        public string noteJson { get; set; }
        public string scene { get; set; }
        public string unlockLevel { get; set; }
        public string levelDesigner { get; set; }
        public string levelDesigner1 { get; set; }
        public string levelDesigner2 { get; set; }
        public string levelDesigner3 { get; set; }
        public string levelDesigner4 { get; set; }
        public string levelDesigner5 { get; set; }
        public string difficulty1 { get; set; }
        public string difficulty2 { get; set; }
        public string difficulty3 { get; set; }
        public string difficulty4 { get; set; }
        public string difficulty5 { get; set; }
    }
}
