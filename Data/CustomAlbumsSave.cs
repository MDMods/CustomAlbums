namespace CustomAlbums.Data
{
    public class CustomAlbumsSave
    {
        public string SelectedAlbum { get; set; } = string.Empty;
        public HashSet<string> Collections { get; set; } = new();
        public HashSet<string> Hides { get; set; } = new();
        public Queue<string> History { get; set; } = new();
        public Dictionary<string, Dictionary<int, CustomChartSave>> Highest { get; set; } = new();
        public Dictionary<string, List<int>> FullCombo { get; set; } = new();
    }
}