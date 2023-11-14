using CustomAlbums.Utilities;
using Il2CppAssets.Scripts.Structs;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2CppSystem.Collections.Generic;

namespace CustomAlbums.Data
{
    public class Talk
    {
        public int Version { get; set; } = 1;
        public Il2CppSystem.Collections.Generic.List<GameDialogArgs> English { get; set; }
        public Il2CppSystem.Collections.Generic.List<GameDialogArgs> ChineseS { get; set; }
        public Il2CppSystem.Collections.Generic.List<GameDialogArgs> ChineseT { get; set; }
        public Il2CppSystem.Collections.Generic.List<GameDialogArgs> Japanese { get; set; }
        public Il2CppSystem.Collections.Generic.List<GameDialogArgs> Korean { get; set; }

        public Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Collections.Generic.List<GameDialogArgs>>.Enumerator GetEnumerator()
        {
            var dict = new Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Collections.Generic.List<GameDialogArgs>>(5);
            dict["English"] = English;
            dict["ChineseS"] = ChineseS;
            dict["ChineseT"] = ChineseT;
            dict["Japanese"] = Japanese;
            dict["Korean"] = Korean;
            return dict.GetEnumerator();
        }
    }
}
