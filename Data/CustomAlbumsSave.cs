using Il2CppPeroTools2.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CustomAlbums.Data
{
    public class CustomAlbumsSave
    {
        public string SelectedAlbum { get; set; } = string.Empty;
        public int SelectedDifficulty { get; set; } = 2;
        public HashSet<string> Collections { get; set; } = new();
        public HashSet<string> Hides { get; set; } = new();
        public Queue<string> History { get; set; } = new();
        public Dictionary<string, Dictionary<int, CustomChartSave>> Highest { get; set; } = new();
        public Dictionary<string, List<int>> FullCombo { get; set; } = new();
    }
}
