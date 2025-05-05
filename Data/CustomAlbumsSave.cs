namespace CustomAlbums.Data
{
    public class CustomAlbumsSave
    {
        public string SelectedAlbum { get; set; } = string.Empty;
        public float Ability { get; set; } = 0;
        public HashSet<string> UnlockedMasters { get; set; } = new();
        public HashSet<string> Collections { get; set; } = new();
        public HashSet<string> Hides { get; set; } = new();
        public List<string> History { get; set; } = new();
        public Dictionary<string, Dictionary<int, ChartSave>> Highest { get; set; } = new();
        public Dictionary<string, List<int>> FullCombo { get; set; } = new();

        internal bool IsEmpty()
        {
            return SelectedAlbum == string.Empty
                && Ability == 0
                && UnlockedMasters.Count == 0
                && Collections.Count == 0
                && Hides.Count == 0
                && History.Count == 0
                && Highest.Count == 0
                && FullCombo.Count == 0;
        }
    }
}