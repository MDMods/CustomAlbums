﻿using System.Text.Json.Serialization;
using CustomAlbums.Utilities;
using static CustomAlbums.Data.SceneEgg;

namespace CustomAlbums.Data
{
    public class AlbumInfo
    {
        public enum HiddenMode
        {
            Click,
            Press,
            Toggle
        }

        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("name_romanized")]
        public string NameRomanized { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string LevelDesigner { get; set; } = string.Empty;
        public string LevelDesigner1 { get; set; } = string.Empty;
        public string LevelDesigner2 { get; set; } = string.Empty;
        public string LevelDesigner3 { get; set; } = string.Empty;
        public string LevelDesigner4 { get; set; } = string.Empty;
        public string LevelDesigner5 { get; set; } = string.Empty;
        public string Bpm { get; set; } = "0";
        public string Scene { get; set; } = "scene_01";
        public SceneEggs SceneEgg { get; set; } = SceneEggs.None;

        public Dictionary<int, string> Difficulties
        {
            get
            {
                var dict = new Dictionary<int, string>();
                if (Difficulty1 != "0") dict.Add(1, Difficulty1);
                if (Difficulty2 != "0") dict.Add(2, Difficulty2);
                if (Difficulty3 != "0") dict.Add(3, Difficulty3);
                if (Difficulty4 != "0") dict.Add(4, Difficulty4);
                if (Difficulty5 != "0") dict.Add(5, Difficulty5);
                return dict;
            }
        }

        public string Difficulty1 { get; set; } = "0";
        public string Difficulty2 { get; set; } = "0";
        public string Difficulty3 { get; set; } = "0";
        public string Difficulty4 { get; set; } = "0";
        public string Difficulty5 { get; set; } = "0";

        public string HideBmsMode { get; set; } = "CLICK";

        [JsonConverter(typeof(Converters.NumberConverter))]
        public string HideBmsDifficulty { get; set; } = "0";

        public string HideBmsMessage { get; set; } = string.Empty;

        public string[] SearchTags { get; set; } = Array.Empty<string>();
    }
}